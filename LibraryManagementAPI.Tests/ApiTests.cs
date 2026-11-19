using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class ApiTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public ApiTests(LibraryApiFactory factory) => _factory = factory;

    private async Task<HttpClient> ClientAsAsync(string? email = null, string? password = null)
    {
        var client = _factory.CreateClient();
        if (email is null) return client;

        var login = await client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto { Email = email, Password = password! });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
        return client;
    }

    private Task<HttpClient> AdminClientAsync() =>
        ClientAsAsync(LibraryApiFactory.AdminEmail, LibraryApiFactory.AdminPassword);

    [Fact]
    public async Task SeededBook_IsListedWithAuthorAndGenre()
    {
        var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<ApiResponse<PagedResult<BookReadDto>>>("/api/v1/Books");

        var book = Assert.Single(response!.Data!.Items, b => b.Id == LibraryContext.SeedBookId);
        Assert.Equal("Adebayo Adeeyo", book.AuthorName);
        Assert.Equal("Fiction", book.GenreName);
    }

    [Fact]
    public async Task GetById_ReturnsAuthorWithBooks_And404ForUnknown()
    {
        var client = _factory.CreateClient();

        var author = await client.GetFromJsonAsync<ApiResponse<AuthorWithBooksDto>>($"/api/v1/Authors/{LibraryContext.SeedAuthorId}");
        Assert.Contains(author!.Data!.Books, b => b.Id == LibraryContext.SeedBookId);

        var missing = await client.GetAsync($"/api/v1/Authors/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Writes_RequireAdmin()
    {
        var anonymous = _factory.CreateClient();
        var genre = new GenreCreateUpdateDto { Name = "Poetry" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/Genres", genre)).StatusCode);

        var register = await anonymous.PostAsJsonAsync("/api/v1/Auth/register",
            new RegisterDto { Email = "reader@test.local", Password = "Reader123", FullName = "Reader" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var user = await ClientAsAsync("reader@test.local", "Reader123");
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/v1/Genres", genre)).StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsErrors()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new RegisterDto { Email = "weak@test.local", Password = "abcdef", FullName = "Weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        Assert.NotEmpty(body!.Errors);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/Auth/login",
            new LoginDto { Email = LibraryApiFactory.AdminEmail, Password = "Wrong123" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateUpdateSearchAndSoftDeleteBook()
    {
        var client = await AdminClientAsync();

        var author = await (await client.PostAsJsonAsync("/api/v1/Authors",
                new AuthorCreateUpdateDto { FirstName = "Chinua", LastName = "Achebe", DateOfBirth = new DateTime(1930, 11, 16) }))
            .Content.ReadFromJsonAsync<ApiResponse<AuthorReadDto>>();
        var genre = await (await client.PostAsJsonAsync("/api/v1/Genres",
                new GenreCreateUpdateDto { Name = "Classics", Description = "Classic literature" }))
            .Content.ReadFromJsonAsync<ApiResponse<GenreReadDto>>();

        var create = await client.PostAsJsonAsync("/api/v1/Books", new BookCreateUpdateDto
        {
            Title = "Things Fall Apart",
            ISBN = "9780385474542",
            PublicationYear = 1958,
            AuthorId = author!.Data!.Id,
            GenreId = genre!.Data!.Id
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var book = (await create.Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>())!.Data!;
        Assert.Equal("Chinua Achebe", book.AuthorName);

        var update = await client.PutAsJsonAsync($"/api/v1/Books/{book.Id}", new BookCreateUpdateDto
        {
            Title = "Things Fall Apart (2nd ed.)",
            ISBN = book.ISBN,
            PublicationYear = 1958,
            AuthorId = book.AuthorId,
            GenreId = book.GenreId
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var search = await client.GetFromJsonAsync<ApiResponse<List<BookReadDto>>>("/api/v1/Books/search?term=Achebe");
        Assert.Contains(search!.Data!, b => b.Id == book.Id && b.Title.EndsWith("(2nd ed.)"));

        // An author with books can't be deleted.
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/Authors/{book.AuthorId}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/Books/{book.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/Books/{book.Id}")).StatusCode);

        // Once the book is gone the author can be deleted.
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/Authors/{book.AuthorId}")).StatusCode);
    }

    [Fact]
    public async Task CreateBook_WithUnknownAuthor_Returns400()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/Books", new BookCreateUpdateDto
        {
            Title = "Ghost",
            ISBN = "123",
            PublicationYear = 2000,
            AuthorId = Guid.NewGuid(),
            GenreId = LibraryContext.SeedGenreId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_WithInvalidYear_Returns400()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/Books", new BookCreateUpdateDto
        {
            Title = "Too old",
            ISBN = "123",
            PublicationYear = 500,
            AuthorId = LibraryContext.SeedAuthorId,
            GenreId = LibraryContext.SeedGenreId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
