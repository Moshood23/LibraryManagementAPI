using LibraryManagementAPI.Data;

namespace LibraryManagementAPI.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LibraryContext _context;
        public IBookRepository Books { get; private set; }
        public IAuthorRepository Authors { get; private set; }
        public IGenreRepository Genres { get; private set; }

        public UnitOfWork(LibraryContext context)
        {
            _context = context;
            Books = new BookRepository(_context);
            Authors = new AuthorRepository(_context);
            Genres = new GenreRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}