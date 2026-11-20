using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories;

public class AuthorRepository : GenericRepository<Author>, IAuthorRepository
{
    public AuthorRepository(LibraryContext context) : base(context) { }

    private IQueryable<Author> WithBooks() =>
        _dbSet.Include(a => a.Books).ThenInclude(b => b.Genre);

    public async Task<Author?> GetByIdWithBooksAsync(Guid id) =>
        await WithBooks().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Author>> SearchAuthorsAsync(string searchTerm) =>
        await MatchingSearch(WithBooks(), searchTerm)
            .OrderBy(a => a.LastName)
            .ToListAsync();

    public async Task<bool> HasBooksAsync(Guid id) =>
        await _context.Books.AnyAsync(b => b.AuthorId == id);

    public async Task<(IEnumerable<Author> Authors, int TotalCount)> GetPagedAsync(AuthorQueryParameters query)
    {
        IQueryable<Author> authors = _dbSet.Include(a => a.Books);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            authors = MatchingSearch(authors, query.Search.Trim());
        }

        var dir = query.SortDirection;
        var ordered = query.SortBy switch
        {
            AuthorSortBy.FirstName => authors.OrderBy(a => a.FirstName, dir).ThenBy(a => a.LastName, dir),
            AuthorSortBy.BookCount => authors.OrderBy(a => a.Books.Count, dir).ThenBy(a => a.LastName),
            AuthorSortBy.Newest => authors.OrderBy(a => a.CreatedAt, dir == SortDirection.Asc ? SortDirection.Desc : SortDirection.Asc),
            _ => authors.OrderBy(a => a.LastName, dir).ThenBy(a => a.FirstName, dir)
        };

        var (items, total) = await ordered.ThenBy(a => a.Id).ToPageAsync(query);
        return (items, total);
    }

    // Lower-casing both sides makes search case-insensitive whatever the database collation is.
    private static IQueryable<Author> MatchingSearch(IQueryable<Author> authors, string term)
    {
        term = term.ToLower();
        return authors.Where(a => a.FirstName.ToLower().Contains(term)
                               || a.LastName.ToLower().Contains(term)
                               || a.Bio.ToLower().Contains(term));
    }
}
