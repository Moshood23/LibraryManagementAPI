using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories;

public class LoanRepository : GenericRepository<Loan>, ILoanRepository
{
    public LoanRepository(LibraryContext context) : base(context) { }

    // Loans are history: keep showing them even after their book has been soft-deleted.
    private IQueryable<Loan> WithDetails() =>
        _dbSet.IgnoreQueryFilters()
            .Where(l => !l.IsDeleted)
            .Include(l => l.Book)
            .Include(l => l.User);

    public async Task<Loan?> GetByIdWithDetailsAsync(Guid id) =>
        await WithDetails().FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IEnumerable<Loan>> GetForUserAsync(string userId, bool includeReturned) =>
        await WithDetails()
            .Where(l => l.UserId == userId && (includeReturned || l.ReturnedAt == null))
            .OrderByDescending(l => l.BorrowedAt)
            .ToListAsync();

    public async Task<IEnumerable<Loan>> GetByStatusAsync(LoanStatusFilter status, DateTime utcNow)
    {
        var query = WithDetails();
        query = status switch
        {
            LoanStatusFilter.Active => query.Where(l => l.ReturnedAt == null),
            LoanStatusFilter.Overdue => query.Where(l => l.ReturnedAt == null && l.DueAt < utcNow),
            LoanStatusFilter.Returned => query.Where(l => l.ReturnedAt != null),
            _ => query
        };
        return await query.OrderBy(l => l.DueAt).ToListAsync();
    }

    public async Task<int> CountActiveForUserAsync(string userId) =>
        await _dbSet.CountAsync(l => l.UserId == userId && l.ReturnedAt == null);

    public async Task<bool> HasActiveLoanAsync(string userId, Guid bookId) =>
        await _dbSet.AnyAsync(l => l.UserId == userId && l.BookId == bookId && l.ReturnedAt == null);

    public async Task<bool> HasOverdueLoansAsync(string userId, DateTime utcNow) =>
        await _dbSet.AnyAsync(l => l.UserId == userId && l.ReturnedAt == null && l.DueAt < utcNow);

    public async Task<bool> BookHasActiveLoansAsync(Guid bookId) =>
        await _dbSet.AnyAsync(l => l.BookId == bookId && l.ReturnedAt == null);
}
