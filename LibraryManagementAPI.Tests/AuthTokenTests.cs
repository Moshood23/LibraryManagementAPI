using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class AuthTokenTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public AuthTokenTests(LibraryApiFactory factory) => _factory = factory;

    private async Task<AuthResponseDto> RegisterAsync(string? email = null)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/Auth/register", new RegisterDto
        {
            Email = email ?? $"user-{Guid.NewGuid():N}@test.local",
            Password = "Member123",
            FullName = "Token Tester"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!;
    }

    private async Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        await _factory.CreateClient().PostAsJsonAsync("/api/v1/Auth/refresh", new RefreshTokenRequestDto { RefreshToken = refreshToken });

    private static async Task<AuthResponseDto> ReadAuthAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!;

    [Fact]
    public async Task RegisterAndLogin_ReturnARefreshToken()
    {
        var registered = await RegisterAsync();

        Assert.False(string.IsNullOrEmpty(registered.RefreshToken));
        Assert.True(registered.RefreshTokenExpiresAt > registered.ExpiresAt);
    }

    [Fact]
    public async Task Refresh_ReturnsNewTokens_ThatWork()
    {
        var registered = await RegisterAsync();

        var response = await RefreshAsync(registered.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await ReadAuthAsync(response);
        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
        Assert.NotEqual(registered.Token, refreshed.Token);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Loans/me")).StatusCode);
    }

    [Fact]
    public async Task ReusingAnOldRefreshToken_Fails_AndRevokesTheNewOneToo()
    {
        var registered = await RegisterAsync();
        var refreshed = await ReadAuthAsync(await RefreshAsync(registered.RefreshToken));

        var reuse = await RefreshAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // Reuse looks like theft, so the legitimate newer token is cut off as well.
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refreshed.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var registered = await RegisterAsync();

        var logout = await _factory.CreateClient().PostAsJsonAsync("/api/v1/Auth/logout",
            new RefreshTokenRequestDto { RefreshToken = registered.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(registered.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task UnknownRefreshToken_Returns401_InApiResponseShape()
    {
        var response = await RefreshAsync("not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        Assert.Equal("Invalid or expired refresh token", body!.Message);
    }

    [Fact]
    public async Task ExpiredRefreshToken_IsRejected()
    {
        var registered = await RegisterAsync();

        _factory.Clock.Advance(TimeSpan.FromDays(8));

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(registered.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount_EvenForTheRightPassword()
    {
        var email = $"guess-{Guid.NewGuid():N}@test.local";
        await RegisterAsync(email);
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto { Email = email, Password = "Wrong1234" });

        var correct = await client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto { Email = email, Password = "Member123" });
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        var body = await correct.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        Assert.Contains("locked", body!.Message);
    }
}
