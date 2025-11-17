using System.Collections;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Repositories
{
    public interface IBookRepository
    {
        Task<IEnumerable<Book>> GetAllAsync();
        Task<Book?> GetByIdAsync(Guid id);
        Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm);
        Task<IEnumerable<Book>> GetBooksByAuthorAsync(Guid authorId);
        Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId);
        Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize);
        Task<Book> AddAsync(Book book);
        Task UpdateAsync(Book book);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task SoftDeleteAsync(Guid id);
    }
}