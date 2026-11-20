using System.Net;
using System.Net.Http.Json;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class RateLimitAndCorsTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public RateLimitAndCorsTests(LibraryApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthEndpoints_AreRateLimited_WithA429InApiResponseShape()
    {
        // A separate app instance with a low limit, so other tests aren't affected.
        var client = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:AuthPermitLimit", "3")).CreateClient();
        var login = new LoginDto { Email = "nobody@test.local", Password = "Whatever1" };

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/Auth/login", login)).StatusCode);

        var blocked = await client.PostAsJsonAsync("/api/v1/Auth/login", login);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        var body = await blocked.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Contains("Too many attempts", body!.Message);

        // Other endpoints aren't limited.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Books")).StatusCode);
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOrigin_Only()
    {
        var client = _factory.CreateClient();

        async Task<HttpResponseMessage> PreflightAsync(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/Books");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            return await client.SendAsync(request);
        }

        var allowed = await PreflightAsync(LibraryApiFactory.AllowedOrigin);
        Assert.Equal(LibraryApiFactory.AllowedOrigin, Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));

        var other = await PreflightAsync("https://evil.example.test");
        Assert.False(other.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
