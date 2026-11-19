namespace LibraryManagementAPI.Repositories;

public interface IUnitOfWork
{
    IBookRepository Books { get; }
    IAuthorRepository Authors { get; }
    IGenreRepository Genres { get; }
    Task<int> SaveChangesAsync();
}
