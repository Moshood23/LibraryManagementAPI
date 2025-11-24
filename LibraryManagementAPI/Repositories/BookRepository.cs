using LibraryManagementAPI.Data;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.EntityFrameworkCore;

public class BookRepository : GenericRepository<Book>, IBookRepository
{

    private readonly LibraryContext _context;
    private readonly DbSet<Book> _books;

    public BookRepository(LibraryContext context) : base(context)
    {
        _books = context.Set<Book>();
    }
    public async Task<IEnumerable<Book>> GetAllWithRelationsAsync()
    {
        return await _books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync (Guid id)
    {
        return await _books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Book>> GetBooksByAuthorAsync(Guid authorId)
    {
        return await _books.Where(b => b.AuthorId == authorId).ToListAsync();
    }

    public Task<Book?> GetByIdWithRelationsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<Book> AddWithRelationsAsync(Book book)
    {
        throw new NotImplementedException();
    }

    public Task UpdateWithRelationsAsync(Book book)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
