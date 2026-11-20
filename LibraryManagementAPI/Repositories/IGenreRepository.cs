using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;

namespace LibraryManagementAPI.Repositories;

public interface IGenreRepository : IGenericRepository<Genre>
{
    Task<Genre?> GetByIdWithBooksAsync(Guid id);
    Task<IEnumerable<Genre>> SearchGenresAsync(string term);
    Task<IEnumerable<Genre>> GetPopularGenresAsync(int topCount);
    Task<bool> HasBooksAsync(Guid id);
    Task<(IEnumerable<Genre> Genres, int TotalCount)> GetPagedAsync(GenreQueryParameters query);
    Task<bool> NameExistsAsync(string name, Guid? excludeGenreId = null);
}
