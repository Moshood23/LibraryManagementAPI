using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class ValidationAndErrorTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public ValidationAndErrorTests(LibraryApiFactory factory) => _factory = factory;

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/Auth/login",
            new LoginDto { Email = LibraryApiFactory.AdminEmail, Password = LibraryApiFactory.AdminPassword });
        var body = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
        return client;
    }

    private static BookCreateUpdateDto Book(string isbn, string title = "Test book") => new()
    {
        Title = title,
        ISBN = isbn,
        PublicationYear = 2000,
        AuthorId = LibraryContext.SeedAuthorId,
        GenreId = LibraryContext.SeedGenreId
    };

    [Fact]
    public async Task InvalidIsbn_Returns400_InApiResponseShape()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/Books", Book("1234567890"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
        Assert.Equal("Validation failed", body.Message);
        Assert.Contains(body.Errors, e => e.Contains("ISBN"));
    }

    [Fact]
    public async Task Isbn_IsStoredWithoutHyphens()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/Books", Book("978-1-86197-876-9"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>();
        Assert.Equal("9781861978769", body!.Data!.ISBN);
    }

    [Fact]
    public async Task DuplicateIsbn_Returns409_EvenWithDifferentFormatting()
    {
        var client = await AdminClientAsync();

        // The seeded book already uses 9780451524935.
        var response = await client.PostAsJsonAsync("/api/v1/Books", Book("978-0-451-52493-5", "Copy of 1984"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdatingBook_KeepingItsOwnIsbn_IsAllowed()
    {
        var client = await AdminClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/Books", Book("0-19-852663-6", "Original")))
            .Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>();

        var response = await client.PutAsJsonAsync($"/api/v1/Books/{created!.Data!.Id}", Book("0198526636", "Renamed"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeletedBook_DoesNotBlockReusingItsIsbn()
    {
        var client = await AdminClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/Books", Book("0-596-52068-9")))
            .Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>();
        await client.DeleteAsync($"/api/v1/Books/{created!.Data!.Id}");

        var response = await client.PostAsJsonAsync("/api/v1/Books", Book("0596520689"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateGenreName_Returns409_IgnoringCase()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/Genres", new GenreCreateUpdateDto { Name = "  fiction " });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RenamingGenreToAnExistingName_Returns409()
    {
        var client = await AdminClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/Genres", new GenreCreateUpdateDto { Name = "History" }))
            .Content.ReadFromJsonAsync<ApiResponse<GenreReadDto>>();

        var rename = await client.PutAsJsonAsync($"/api/v1/Genres/{created!.Data!.Id}", new GenreCreateUpdateDto { Name = "FICTION" });
        Assert.Equal(HttpStatusCode.Conflict, rename.StatusCode);

        var keepOwnName = await client.PutAsJsonAsync($"/api/v1/Genres/{created.Data.Id}",
            new GenreCreateUpdateDto { Name = "History", Description = "Updated" });
        Assert.Equal(HttpStatusCode.OK, keepOwnName.StatusCode);
    }

    [Fact]
    public async Task MissingToken_Returns401_InApiResponseShape()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/Genres", new GenreCreateUpdateDto { Name = "Anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
        Assert.Contains("Authentication required", body.Message);
    }

    [Fact]
    public async Task NonAdmin_Gets403_InApiResponseShape()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new RegisterDto { Email = "member@test.local", Password = "Member123", FullName = "Member" });
        var token = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"/api/v1/Books/{LibraryContext.SeedBookId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Contains("permission", body!.Message);
    }

    [Fact]
    public async Task MissingRequiredFields_Returns400_WithEachError()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/Authors", new { firstName = "", lastName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Contains(body!.Errors, e => e.StartsWith("FirstName"));
        Assert.Contains(body.Errors, e => e.StartsWith("LastName"));
    }
}
