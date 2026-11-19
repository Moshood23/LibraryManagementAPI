using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repositories;

public interface IGenreRepository : IGenericRepository<Genre>
{
    Task<IEnumerable<Genre>> GetAllWithBooksAsync();
    Task<Genre?> GetByIdWithBooksAsync(Guid id);
    Task<IEnumerable<Genre>> SearchGenresAsync(string term);
    Task<IEnumerable<Genre>> GetPopularGenresAsync(int topCount);
    Task<bool> HasBooksAsync(Guid id);
    Task<bool> NameExistsAsync(string name, Guid? excludeGenreId = null);
}
