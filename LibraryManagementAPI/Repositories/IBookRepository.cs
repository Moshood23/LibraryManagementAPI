using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repositories;

public interface IBookRepository : IGenericRepository<Book>
{
    Task<IEnumerable<Book>> GetAllWithRelationsAsync();
    Task<Book?> GetByIdWithRelationsAsync(Guid id);
    Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm);
    Task<IEnumerable<Book>> GetBooksByAuthorAsync(Guid authorId);
    Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId);
    Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize);
    Task<bool> IsbnExistsAsync(string isbn, Guid? excludeBookId = null);
}
