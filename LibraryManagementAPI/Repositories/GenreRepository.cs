using LibraryManagementAPI.Data;
using LibraryManagementAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories
{
    public class GenreRepository : IGenreRepository
    {
        private readonly LibraryContext _context;
        private readonly DbSet<Genre> _genres;

        public GenreRepository(LibraryContext context)
        {
            _genres = context.Set<Genre>();
        }

        public async Task<IEnumerable<Genre>> GetAllAsync()
        {
            return await _genres.Include(g => g.Books).ToListAsync();
        }

        public async Task<Genre?> GetByIdAsync(Guid id)
        {
            return await _genres
                .Include(g => g.Books)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<IEnumerable<Genre>> SearchAsync(string term)
        {
            return await _genres
                .Where(g => g.Name.Contains(term) || g.Description.Contains(term))
                .ToListAsync();
        }

        public async Task<Genre> AddAsync(Genre genre)
        {
            await _genres.AddAsync(genre);
            await _context.SaveChangesAsync();
            return genre;
        }

        public async Task UpdateAsync(Genre genre)
        {
            _genres.Update(genre);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var genre = await _genres.FindAsync(id);
            if (genre == null) return false;

            _genres.Remove(genre);
            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<IEnumerable<Genre>> GetPopularGenresAsync(int topCount)
        {
            return await _genres
                .Include(g => g.Books)
                .OrderByDescending(g => g.Books.Count)
                .Take(topCount)
                .ToListAsync();
        }

        public async Task<int> GetBookCountByGenreAsync(Guid genreId)
        {
            var genre = await _genres
                .Include(g => g.Books)
                .FirstOrDefaultAsync(g => g.Id == genreId);

            return genre?.Books.Count ?? 0;
        }


        public async Task SoftDeleteAsync(Guid id)
        {
            var genre = await _genres.IgnoreQueryFilters()
                                     .FirstOrDefaultAsync(g => g.Id == id);
            if (genre != null)
            {
                genre.IsDeleted = true;
                genre.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }



        public Task<Genre?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Genre>> GetGenresWithBooksAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Genre?> GetGenreWithBooksAsync(int genreId)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetBookCountByGenreAsync(int genreId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task SoftDeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task GetGenreWithBooksAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}