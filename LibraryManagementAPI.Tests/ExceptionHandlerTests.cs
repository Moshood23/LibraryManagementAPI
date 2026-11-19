using System.Net;
using System.Net.Http.Json;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class ExceptionHandlerTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public ExceptionHandlerTests(LibraryApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UnhandledException_Returns500_InApiResponseShape_WithoutDetails()
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
                services.AddScoped<IUnitOfWork>(_ => throw new InvalidOperationException("secret internal detail"))))
            .CreateClient();

        var response = await client.GetAsync("/api/v1/Books");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
        Assert.Equal("An unexpected error occurred.", body.Message);
        Assert.DoesNotContain(body.Errors, e => e.Contains("secret internal detail"));
    }
}
