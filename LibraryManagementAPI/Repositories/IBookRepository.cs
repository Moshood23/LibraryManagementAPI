using System.Collections;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Repositories
{
    public interface IBookRepository : IGenericRepository<Book>
    {
        Task<IEnumerable<Book>> GetAllWithRelationsAsync();
        Task<Book?> GetByIdWithRelationsAsync(Guid id);
        Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm);
        Task<IEnumerable<Book>> GetBooksByAuthorAsync(Guid authorId);
        Task<IEnumerable<Book>> GetBooksByGenreAsync(Guid genreId);
        Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize);
        Task<Book> AddWithRelationsAsync(Book book);
        Task UpdateWithRelationsAsync(Book book);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task UpdateAsync(Book book);
        Task<Book?> GetByIdAsync(Guid id);
    }
}