using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories;

public class AuthorRepository : GenericRepository<Author>, IAuthorRepository
{
    public AuthorRepository(LibraryContext context) : base(context) { }

    private IQueryable<Author> WithBooks() =>
        _dbSet.Include(a => a.Books).ThenInclude(b => b.Genre);

    public async Task<IEnumerable<Author>> GetAllWithBooksAsync() =>
        await WithBooks().OrderBy(a => a.LastName).ThenBy(a => a.FirstName).ToListAsync();

    public async Task<Author?> GetByIdWithBooksAsync(Guid id) =>
        await WithBooks().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Author>> SearchAuthorsAsync(string searchTerm) =>
        await WithBooks()
            .Where(a => a.FirstName.Contains(searchTerm)
                     || a.LastName.Contains(searchTerm)
                     || a.Bio.Contains(searchTerm))
            .OrderBy(a => a.LastName)
            .ToListAsync();

    public async Task<bool> HasBooksAsync(Guid id) =>
        await _context.Books.AnyAsync(b => b.AuthorId == id);
}
