using System.Collections;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Repositories
{
    public interface IAuthorRepository : IRepository
    {
        Task<IEnumerable<Author>> GetAuthorsWithBooksAsync();
        Task<Author?> GetAuthorWithBooksAsync(Guid authorId);

        Task<Author> AddAsync(Author author);
        Task<Author> UpdateAsync(Author author);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<Author>> GetAllAsync();
        Task<Author> GetByIdAsync(Guid id);
        Task SoftDeleteAsync(Guid id);
    }
}
