using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Repositories
{
    public interface IGenreRepository
    {
        Task<IEnumerable<Genre>> GetAllAsync();
        Task<Genre?> GetByIdAsync(int id);
        Task<IEnumerable<Genre>> GetGenresWithBooksAsync();
        Task<Genre?> GetGenreWithBooksAsync(int genreId);
        Task<IEnumerable<Genre>> GetPopularGenresAsync(int topCount);
        Task<int> GetBookCountByGenreAsync(int genreId);
        Task<IEnumerable<Genre>> SearchAsync(string searchTerm);
        Task<Genre> AddAsync(Genre genre);
        Task UpdateAsync(Genre genre);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task SoftDeleteAsync(int id);
        Task<Genre> GetByIdAsync(Guid id);
        Task GetGenreWithBooksAsync(Guid id);
    }
}