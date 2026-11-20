using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;

namespace LibraryManagementAPI.Repositories;

public interface IAuthorRepository : IGenericRepository<Author>
{
    Task<Author?> GetByIdWithBooksAsync(Guid id);
    Task<IEnumerable<Author>> SearchAuthorsAsync(string searchTerm);
    Task<bool> HasBooksAsync(Guid id);
    Task<(IEnumerable<Author> Authors, int TotalCount)> GetPagedAsync(AuthorQueryParameters query);
}
