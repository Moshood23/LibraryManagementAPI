using LibraryManagementAPI.Data;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.EntityFrameworkCore;

public class BookRepository : IBookRepository
{

    private readonly LibraryContext _context;
    private readonly DbSet<Book> _books;

    public BookRepository(LibraryContext context) 
    {
        _books = context.Set<Book>();
    }
    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _books
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(Guid id)
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

    public async Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId)
    {
        return await _books.Where(b => b.GenreId == genreId).ToListAsync();
    }

    public async Task<Book> AddAsync(Book book)
    {
        await _books.AddAsync(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<Book> UpdateAsync(Book book)
    {
        _books.Update(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var book = await _books.FindAsync(id);
        if (book == null) return false;

        _books.Remove(book);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    Task IBookRepository.UpdateAsync(Book book)
    {
        return UpdateAsync(book);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task SoftDeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
