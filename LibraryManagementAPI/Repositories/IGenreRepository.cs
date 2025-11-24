using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Repositories
{
    public interface IGenreRepository : IGenericRepository<Genre>
    {
        Task<IEnumerable<Genre>> GetAllAsync();
        Task<Genre?> GetAllWithBooksAsync(Guid id);
        Task<IEnumerable<object>> GetAllWithBooksAsync();
        Task<Genre> GetByIdAsync(Guid id);
        Task GetByIdWithBooksAsync(Guid id);
        Task<IEnumerable<Genre>> GetGenresWithBooksAsync();
        Task<IEnumerable<object>> SearchGenresAsync(string term);
    }
}