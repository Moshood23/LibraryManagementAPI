using System.Net;
using System.Net.Http.Json;
using LibraryManagementAPI.DTOs;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class BrowsingTests : IClassFixture<LibraryApiFactory>
{
    private readonly LibraryApiFactory _factory;

    public BrowsingTests(LibraryApiFactory factory) => _factory = factory;

    /// <summary>A fresh author and genre, so each test only sees the books it creates itself.</summary>
    private async Task<(HttpClient Admin, Guid AuthorId, Guid GenreId)> FreshShelfAsync(string lastName = "Shelf")
    {
        var admin = await TestApi.AdminAsync(_factory);
        var author = (await (await admin.PostAsJsonAsync("/api/v1/Authors",
                new AuthorCreateUpdateDto { FirstName = "Test", LastName = lastName, DateOfBirth = new DateTime(1970, 1, 1) }))
            .Content.ReadFromJsonAsync<ApiResponse<AuthorReadDto>>())!.Data!;
        var genre = (await (await admin.PostAsJsonAsync("/api/v1/Genres",
                new GenreCreateUpdateDto { Name = $"Genre {Guid.NewGuid():N}" }))
            .Content.ReadFromJsonAsync<ApiResponse<GenreReadDto>>())!.Data!;
        return (admin, author.Id, genre.Id);
    }

    private static async Task<BookReadDto> AddBookAsync(HttpClient admin, Guid authorId, Guid genreId,
        string title, int year, int copies = 1)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/Books", new BookCreateUpdateDto
        {
            Title = title,
            ISBN = TestApi.RandomIsbn13(),
            PublicationYear = year,
            TotalCopies = copies,
            AuthorId = authorId,
            GenreId = genreId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<BookReadDto>>())!.Data!;
    }

    private async Task<PagedResult<T>> GetPageAsync<T>(string url) =>
        (await _factory.CreateClient().GetFromJsonAsync<ApiResponse<PagedResult<T>>>(url))!.Data!;

    [Fact]
    public async Task FilterByGenre_AndYearRange_SortedByYearDescending()
    {
        var (admin, authorId, genreId) = await FreshShelfAsync();
        await AddBookAsync(admin, authorId, genreId, "Old", 1950);
        await AddBookAsync(admin, authorId, genreId, "Middle", 1980);
        await AddBookAsync(admin, authorId, genreId, "Recent", 2010);
        await AddBookAsync(admin, authorId, genreId, "Newest", 2020);

        var page = await GetPageAsync<BookReadDto>(
            $"/api/v1/Books?genreId={genreId}&yearFrom=1980&yearTo=2015&sortBy=Year&sortDirection=Desc");

        Assert.Equal(new[] { "Recent", "Middle" }, page.Items.Select(b => b.Title));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task FilterByAuthor_AndAvailability()
    {
        var (admin, authorId, genreId) = await FreshShelfAsync();
        var onShelf = await AddBookAsync(admin, authorId, genreId, "On shelf", 2000, copies: 2);
        var none = await AddBookAsync(admin, authorId, genreId, "No copies", 2000, copies: 0);

        var available = await GetPageAsync<BookReadDto>($"/api/v1/Books?authorId={authorId}&available=true");
        Assert.Equal(new[] { onShelf.Id }, available.Items.Select(b => b.Id));

        var unavailable = await GetPageAsync<BookReadDto>($"/api/v1/Books?authorId={authorId}&available=false");
        Assert.Equal(new[] { none.Id }, unavailable.Items.Select(b => b.Id));
    }

    [Fact]
    public async Task BorrowingTheLastCopy_MovesBookToUnavailable()
    {
        var (admin, authorId, genreId) = await FreshShelfAsync();
        var book = await AddBookAsync(admin, authorId, genreId, "Single copy", 2000, copies: 1);
        var member = await TestApi.NewMemberAsync(_factory);

        (await member.PostAsync($"/api/v1/Books/{book.Id}/borrow", null)).EnsureSuccessStatusCode();

        var available = await GetPageAsync<BookReadDto>($"/api/v1/Books?authorId={authorId}&available=true");
        Assert.Empty(available.Items);
    }

    [Fact]
    public async Task Search_MatchesTitleAndCombinesWithOtherFilters()
    {
        var (admin, authorId, genreId) = await FreshShelfAsync();
        await AddBookAsync(admin, authorId, genreId, "The Quiet Harbour", 1999);
        await AddBookAsync(admin, authorId, genreId, "Loud City", 1999);

        var page = await GetPageAsync<BookReadDto>($"/api/v1/Books?genreId={genreId}&search=harbour");

        Assert.Equal("The Quiet Harbour", Assert.Single(page.Items).Title);
    }

    [Fact]
    public async Task Paging_ReturnsTheRightSlice_AndNextPreviousFlags()
    {
        var (admin, authorId, genreId) = await FreshShelfAsync();
        foreach (var title in new[] { "A", "B", "C", "D", "E" })
            await AddBookAsync(admin, authorId, genreId, title, 2000);

        var first = await GetPageAsync<BookReadDto>($"/api/v1/Books?genreId={genreId}&pageSize=2&page=1");
        var last = await GetPageAsync<BookReadDto>($"/api/v1/Books?genreId={genreId}&pageSize=2&page=3");

        Assert.Equal(new[] { "A", "B" }, first.Items.Select(b => b.Title));
        Assert.Equal(5, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.False(first.HasPreviousPage);
        Assert.True(first.HasNextPage);

        Assert.Equal(new[] { "E" }, last.Items.Select(b => b.Title));
        Assert.True(last.HasPreviousPage);
        Assert.False(last.HasNextPage);
    }

    [Fact]
    public async Task SortNames_AreCaseInsensitive()
    {
        var (admin, authorId, genreId) = await FreshShelfAsync();
        await AddBookAsync(admin, authorId, genreId, "Alpha", 2000);
        await AddBookAsync(admin, authorId, genreId, "Beta", 2000);

        var page = await GetPageAsync<BookReadDto>($"/api/v1/Books?genreId={genreId}&sortby=title&sortdirection=desc");

        Assert.Equal(new[] { "Beta", "Alpha" }, page.Items.Select(b => b.Title));
    }

    [Theory]
    [InlineData("yearFrom=2000&yearTo=1990", "YearFrom")]
    [InlineData("pageSize=500", "PageSize")]
    [InlineData("page=0", "Page")]
    [InlineData("sortBy=Popularity", "sortBy")]
    [InlineData("sortBy=42", "SortBy")]
    [InlineData("sortDirection=sideways", "sortDirection")]
    public async Task InvalidQueries_Return400_WithAClearError(string query, string mentions)
    {
        var response = await _factory.CreateClient().GetAsync($"/api/v1/Books?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Contains(body!.Errors, e => e.Contains(mentions, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Authors_ArePaged_Searchable_AndSortableByBookCount()
    {
        var unique = $"Zz{Guid.NewGuid():N}"[..12];
        var (admin, busyAuthor, genreId) = await FreshShelfAsync(unique + "Busy");
        var (_, quietAuthor, _) = await FreshShelfAsync(unique + "Quiet");
        await AddBookAsync(admin, busyAuthor, genreId, "One", 2000);
        await AddBookAsync(admin, busyAuthor, genreId, "Two", 2000);
        await AddBookAsync(admin, quietAuthor, genreId, "Three", 2000);

        var page = await GetPageAsync<AuthorReadDto>(
            $"/api/v1/Authors?search={unique}&sortBy=BookCount&sortDirection=Desc&pageSize=1");

        Assert.Equal(2, page.TotalCount);
        var top = Assert.Single(page.Items);
        Assert.Equal(busyAuthor, top.Id);
        Assert.Equal(2, top.BookCount);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task Genres_ArePaged_AndSortedByName()
    {
        var admin = await TestApi.AdminAsync(_factory);
        var prefix = $"Yy{Guid.NewGuid():N}"[..10];
        foreach (var name in new[] { "Gamma", "Alpha", "Beta" })
            await admin.PostAsJsonAsync("/api/v1/Genres", new GenreCreateUpdateDto { Name = $"{prefix} {name}" });

        var page = await GetPageAsync<GenreReadDto>($"/api/v1/Genres?search={prefix}&pageSize=2");

        Assert.Equal(new[] { $"{prefix} Alpha", $"{prefix} Beta" }, page.Items.Select(g => g.Name));
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task DefaultListing_StillWorksWithNoParameters()
    {
        var books = await GetPageAsync<BookReadDto>("/api/v1/Books");
        var authors = await GetPageAsync<AuthorReadDto>("/api/v1/Authors");
        var genres = await GetPageAsync<GenreReadDto>("/api/v1/Genres");

        Assert.Equal(1, books.Page);
        Assert.Equal(20, books.PageSize);
        Assert.NotEmpty(books.Items);
        Assert.NotEmpty(authors.Items);
        Assert.NotEmpty(genres.Items);
    }
}
