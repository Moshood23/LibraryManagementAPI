using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories;

public class GenreRepository : GenericRepository<Genre>, IGenreRepository
{
    public GenreRepository(LibraryContext context) : base(context) { }

    private IQueryable<Genre> WithBooks() =>
        _dbSet.Include(g => g.Books).ThenInclude(b => b.Author);

    public async Task<Genre?> GetByIdWithBooksAsync(Guid id) =>
        await WithBooks().FirstOrDefaultAsync(g => g.Id == id);

    public async Task<IEnumerable<Genre>> SearchGenresAsync(string term) =>
        await MatchingSearch(WithBooks(), term)
            .OrderBy(g => g.Name)
            .ToListAsync();

    public async Task<IEnumerable<Genre>> GetPopularGenresAsync(int topCount) =>
        await WithBooks()
            .OrderByDescending(g => g.Books.Count)
            .Take(topCount)
            .ToListAsync();

    public async Task<bool> HasBooksAsync(Guid id) =>
        await _context.Books.AnyAsync(b => b.GenreId == id);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeGenreId = null)
    {
        var lowered = name.Trim().ToLower();
        return await _dbSet.AnyAsync(g => g.Name.ToLower() == lowered && g.Id != excludeGenreId);
    }

    public async Task<(IEnumerable<Genre> Genres, int TotalCount)> GetPagedAsync(GenreQueryParameters query)
    {
        IQueryable<Genre> genres = _dbSet.Include(g => g.Books);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            genres = MatchingSearch(genres, query.Search.Trim());
        }

        var dir = query.SortDirection;
        var ordered = query.SortBy switch
        {
            GenreSortBy.BookCount => genres.OrderBy(g => g.Books.Count, dir).ThenBy(g => g.Name),
            GenreSortBy.Newest => genres.OrderBy(g => g.CreatedAt, dir == SortDirection.Asc ? SortDirection.Desc : SortDirection.Asc),
            _ => genres.OrderBy(g => g.Name, dir)
        };

        var (items, total) = await ordered.ThenBy(g => g.Id).ToPageAsync(query);
        return (items, total);
    }

    // Lower-casing both sides makes search case-insensitive whatever the database collation is.
    private static IQueryable<Genre> MatchingSearch(IQueryable<Genre> genres, string term)
    {
        term = term.ToLower();
        return genres.Where(g => g.Name.ToLower().Contains(term) || g.Description.ToLower().Contains(term));
    }
}
