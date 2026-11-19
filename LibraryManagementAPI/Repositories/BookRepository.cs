using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
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
        await WithRelations()
            .Where(b => b.Title.Contains(searchTerm)
                     || b.ISBN.Contains(searchTerm)
                     || b.Description.Contains(searchTerm)
                     || b.Author.FirstName.Contains(searchTerm)
                     || b.Author.LastName.Contains(searchTerm)
                     || b.Genre.Name.Contains(searchTerm))
            .OrderBy(b => b.Title)
            .ToListAsync();

    public async Task<IEnumerable<Book>> GetBooksByAuthorAsync(Guid authorId) =>
        await WithRelations().Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToListAsync();

    public async Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId) =>
        await WithRelations().Where(b => b.GenreId == genreId).OrderBy(b => b.Title).ToListAsync();

    public async Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize)
    {
        var total = await _dbSet.CountAsync();
        var books = await WithRelations()
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (books, total);
    }

    public async Task<bool> IsbnExistsAsync(string isbn, Guid? excludeBookId = null) =>
        await _dbSet.AnyAsync(b => b.ISBN == isbn && b.Id != excludeBookId);
}
