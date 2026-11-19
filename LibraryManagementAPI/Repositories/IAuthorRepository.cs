using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repositories;

public interface IAuthorRepository : IGenericRepository<Author>
{
    Task<IEnumerable<Author>> GetAllWithBooksAsync();
    Task<Author?> GetByIdWithBooksAsync(Guid id);
    Task<IEnumerable<Author>> SearchAuthorsAsync(string searchTerm);
    Task<bool> HasBooksAsync(Guid id);
}
