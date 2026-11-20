using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(LibraryContext context) : base(context) { }

    private IQueryable<Book> WithRelations() =>
        _dbSet.Include(b => b.Author).Include(b => b.Genre);

    public async Task<IEnumerable<Book>> GetAllWithRelationsAsync() =>
        await WithRelations().OrderBy(b => b.Title).ToListAsync();

    public async Task<Book?> GetByIdWithRelationsAsync(Guid id) =>
        await WithRelations().FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm) =>
        await MatchingSearch(WithRelations(), searchTerm)
            .OrderBy(b => b.Title)
            .ToListAsync();

    // Lower-casing both sides makes search case-insensitive whatever the database collation is.
    private static IQueryable<Book> MatchingSearch(IQueryable<Book> books, string term)
    {
        term = term.ToLower();
        return books.Where(b => b.Title.ToLower().Contains(term)
                             || b.ISBN.ToLower().Contains(term)
                             || b.Description.ToLower().Contains(term)
                             || b.Author.FirstName.ToLower().Contains(term)
                             || b.Author.LastName.ToLower().Contains(term)
                             || b.Genre.Name.ToLower().Contains(term));
    }

    public async Task<IEnumerable<Book>> GetBooksByAuthorAsync(Guid authorId) =>
        await WithRelations().Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToListAsync();

    public async Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId) =>
        await WithRelations().Where(b => b.GenreId == genreId).OrderBy(b => b.Title).ToListAsync();

    public async Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(BookQueryParameters query)
    {
        var books = WithRelations();

        if (!string.IsNullOrWhiteSpace(query.Search))
            books = MatchingSearch(books, query.Search.Trim());
        if (query.AuthorId is { } authorId)
            books = books.Where(b => b.AuthorId == authorId);
        if (query.GenreId is { } genreId)
            books = books.Where(b => b.GenreId == genreId);
        if (query.YearFrom is { } from)
            books = books.Where(b => b.PublicationYear >= from);
        if (query.YearTo is { } to)
            books = books.Where(b => b.PublicationYear <= to);
        if (query.Available is { } available)
            books = available ? books.Where(b => b.AvailableCopies > 0) : books.Where(b => b.AvailableCopies == 0);

        var dir = query.SortDirection;
        var ordered = query.SortBy switch
        {
            BookSortBy.Year => books.OrderBy(b => b.PublicationYear, dir).ThenBy(b => b.Title),
            BookSortBy.Author => books.OrderBy(b => b.Author.LastName, dir).ThenBy(b => b.Author.FirstName, dir).ThenBy(b => b.Title),
            BookSortBy.Newest => books.OrderBy(b => b.CreatedAt, dir == SortDirection.Asc ? SortDirection.Desc : SortDirection.Asc),
            _ => books.OrderBy(b => b.Title, dir)
        };

        // Id as the final tie-breaker keeps pages stable when many rows share a sort value.
        var (items, total) = await ordered.ThenBy(b => b.Id).ToPageAsync(query);
        return (items, total);
    }

    public async Task<bool> IsbnExistsAsync(string isbn, Guid? excludeBookId = null) =>
        await _dbSet.AnyAsync(b => b.ISBN == isbn && b.Id != excludeBookId);
}
