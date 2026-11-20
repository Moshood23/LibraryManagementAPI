using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class UserAdminTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public UserAdminTests(LibraryApiFactory factory) => _factory = factory;

    private async Task<(string Email, AuthResponseDto Auth)> RegisterAsync()
    {
        var email = $"member-{Guid.NewGuid():N}@test.local";
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/Auth/register",
            new RegisterDto { Email = email, Password = "Member123", FullName = "Admin Test Member" });
        response.EnsureSuccessStatusCode();
        return (email, (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!);
    }

    private static async Task<UserReadDto> FindAsync(HttpClient admin, string email)
    {
        var page = (await admin.GetFromJsonAsync<ApiResponse<PagedResult<UserReadDto>>>($"/api/v1/Users?search={email}"))!.Data!;
        return Assert.Single(page.Items);
    }

    private HttpClient WithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Admin_CanListAndSearchUsers()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var (email, _) = await RegisterAsync();

        var user = await FindAsync(admin, email);

        Assert.Equal(email, user.Email);
        Assert.Equal(new[] { "User" }, user.Roles);
        Assert.False(user.IsLockedOut);
    }

    [Fact]
    public async Task NonAdmins_CannotUseUserAdministration()
    {
        var (_, auth) = await RegisterAsync();
        var member = WithToken(auth.Token);

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/v1/Users")).StatusCode);
    }

    [Fact]
    public async Task Promotion_TakesEffectAfterRefresh()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var (email, auth) = await RegisterAsync();
        var user = await FindAsync(admin, email);

        var promote = await admin.PutAsJsonAsync($"/api/v1/Users/{user.Id}/roles",
            new UpdateUserRolesDto { Roles = new() { "user", "ADMIN" } });
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);
        var promoted = (await promote.Content.ReadFromJsonAsync<ApiResponse<UserReadDto>>())!.Data!;
        Assert.Equal(new[] { "Admin", "User" }, promoted.Roles);

        // The old access token still carries the old roles...
        var genre = new GenreCreateUpdateDto { Name = $"Promo {Guid.NewGuid():N}" };
        Assert.Equal(HttpStatusCode.Forbidden, (await WithToken(auth.Token).PostAsJsonAsync("/api/v1/Genres", genre)).StatusCode);

        // ...a refreshed one carries the new ones.
        var refreshed = (await (await _factory.CreateClient().PostAsJsonAsync("/api/v1/Auth/refresh",
                new RefreshTokenRequestDto { RefreshToken = auth.RefreshToken }))
            .Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!;
        Assert.Contains("Admin", refreshed.Roles);
        Assert.Equal(HttpStatusCode.Created, (await WithToken(refreshed.Token).PostAsJsonAsync("/api/v1/Genres", genre)).StatusCode);
    }

    [Fact]
    public async Task UnknownRole_Returns400()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var (email, _) = await RegisterAsync();
        var user = await FindAsync(admin, email);

        var response = await admin.PutAsJsonAsync($"/api/v1/Users/{user.Id}/roles",
            new UpdateUserRolesDto { Roles = new() { "Superuser" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotDemoteOrLockThemselves()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var self = await FindAsync(admin, LibraryApiFactory.AdminEmail);

        var demote = await admin.PutAsJsonAsync($"/api/v1/Users/{self.Id}/roles", new UpdateUserRolesDto { Roles = new() { "User" } });
        var lockSelf = await admin.PostAsync($"/api/v1/Users/{self.Id}/lock", null);

        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, lockSelf.StatusCode);
    }

    [Fact]
    public async Task LockedUser_CannotLoginOrRefresh_UntilUnlocked()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var (email, auth) = await RegisterAsync();
        var user = await FindAsync(admin, email);
        var anonymous = _factory.CreateClient();

        var locked = await admin.PostAsync($"/api/v1/Users/{user.Id}/lock", null);
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);
        Assert.True((await locked.Content.ReadFromJsonAsync<ApiResponse<UserReadDto>>())!.Data!.IsLockedOut);

        var login = new LoginDto { Email = email, Password = "Member123" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/Auth/login", login)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/Auth/refresh",
            new RefreshTokenRequestDto { RefreshToken = auth.RefreshToken })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/Users/{user.Id}/unlock", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/v1/Auth/login", login)).StatusCode);
    }

    [Fact]
    public async Task UnknownUser_Returns404()
    {
        var admin = await TestApi.AdminAsync(_factory);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/Users/{Guid.NewGuid()}")).StatusCode);
    }
}
