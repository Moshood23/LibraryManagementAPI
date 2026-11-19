namespace LibraryManagementAPI.Repositories;

public interface IUnitOfWork
{
    IBookRepository Books { get; }
    IAuthorRepository Authors { get; }
    IGenreRepository Genres { get; }
    ILoanRepository Loans { get; }
    Task<int> SaveChangesAsync();

    /// <summary>Discards every pending change, e.g. before retrying after a concurrency conflict.</summary>
    void ClearChanges();
}
