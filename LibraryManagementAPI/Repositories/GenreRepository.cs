using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories;

public class GenreRepository : GenericRepository<Genre>, IGenreRepository
{
    public GenreRepository(LibraryContext context) : base(context) { }

    private IQueryable<Genre> WithBooks() =>
        _dbSet.Include(g => g.Books).ThenInclude(b => b.Author);

    public async Task<IEnumerable<Genre>> GetAllWithBooksAsync() =>
        await WithBooks().OrderBy(g => g.Name).ToListAsync();

    public async Task<Genre?> GetByIdWithBooksAsync(Guid id) =>
        await WithBooks().FirstOrDefaultAsync(g => g.Id == id);

    public async Task<IEnumerable<Genre>> SearchGenresAsync(string term) =>
        await WithBooks()
            .Where(g => g.Name.Contains(term) || g.Description.Contains(term))
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
}
