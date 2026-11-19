using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repositories;

public enum LoanStatusFilter
{
    Active,
    Overdue,
    Returned,
    All
}

public interface ILoanRepository : IGenericRepository<Loan>
{
    Task<Loan?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Loan>> GetForUserAsync(string userId, bool includeReturned);
    Task<IEnumerable<Loan>> GetByStatusAsync(LoanStatusFilter status, DateTime utcNow);
    Task<int> CountActiveForUserAsync(string userId);
    Task<bool> HasActiveLoanAsync(string userId, Guid bookId);
    Task<bool> HasOverdueLoansAsync(string userId, DateTime utcNow);
    Task<bool> BookHasActiveLoansAsync(Guid bookId);
}
