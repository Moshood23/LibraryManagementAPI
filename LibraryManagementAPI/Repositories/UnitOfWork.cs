using LibraryManagementAPI.Data;

namespace LibraryManagementAPI.Repositories;

/// <summary>
/// Groups the repositories over one scoped <see cref="LibraryContext"/>.
/// The DI container owns the context's lifetime, so this class does not dispose it.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly LibraryContext _context;

    public UnitOfWork(LibraryContext context)
    {
        _context = context;
        Books = new BookRepository(context);
        Authors = new AuthorRepository(context);
        Genres = new GenreRepository(context);
    }

    public IBookRepository Books { get; }
    public IAuthorRepository Authors { get; }
    public IGenreRepository Genres { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
