using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.DTOs;

namespace LibraryManagementAPI.Tests;

/// <summary>Shortcuts for setting up users and books in integration tests.</summary>
public static class TestApi
{
    public static async Task<HttpClient> AdminAsync(LibraryApiFactory factory) =>
        await LoginAsync(factory, LibraryApiFactory.AdminEmail, LibraryApiFactory.AdminPassword);

    /// <summary>Registers a brand-new member and returns a client signed in as them.</summary>
    public static async Task<HttpClient> NewMemberAsync(LibraryApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new RegisterDto { Email = email, Password = "Member123", FullName = "Test Member" });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<HttpClient> LoginAsync(LibraryApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Creates a book with a random valid ISBN-13.</summary>
    public static async Task<BookReadDto> CreateBookAsync(HttpClient admin, int totalCopies = 1, string title = "Loan test book")
    {
        var response = await admin.PostAsJsonAsync("/api/v1/Books", new BookCreateUpdateDto
        {
            Title = title,
            ISBN = RandomIsbn13(),
            PublicationYear = 2001,
            TotalCopies = totalCopies,
            AuthorId = LibraryContext.SeedAuthorId,
            GenreId = LibraryContext.SeedGenreId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>())!.Data!;
    }

    public static async Task<BookReadDto> GetBookAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<ApiResponse<BookReadDto>>($"/api/v1/Books/{id}"))!.Data!;

    public static string RandomIsbn13()
    {
        var digits = "978" + string.Concat(Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)));
        var sum = digits.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return digits + (10 - sum % 10) % 10;
    }
}
