using System.Collections;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Repositories
{
    public interface IAuthorRepository : IGenericRepository<Author>
    {
        Task<IEnumerable<Author>> GetAllWithBooksAsync();
        Task<IEnumerable<Author>> GetAuthorsWithBooksAsync();
        Task<Author?> GetAuthorWithBooksAsync(Guid authorId);
        Task GetByIdWithBooksAsync(Guid id);
        Task<IEnumerable<Author>> SearchAuthorsAsync(string searchTerm);

    }   
}
