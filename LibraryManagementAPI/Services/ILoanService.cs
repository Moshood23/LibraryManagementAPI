using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Repositories;

namespace LibraryManagementAPI.Services;

public interface ILoanService
{
    Task<ServiceResult<LoanReadDto>> BorrowAsync(Guid bookId, string userId);
    Task<ServiceResult<LoanReadDto>> ReturnAsync(Guid loanId, string userId, bool isAdmin);
    Task<ServiceResult<LoanReadDto>> GetAsync(Guid loanId, string userId, bool isAdmin);
    Task<IReadOnlyList<LoanReadDto>> GetForUserAsync(string userId, bool includeReturned);
    Task<IReadOnlyList<LoanReadDto>> GetByStatusAsync(LoanStatusFilter status);
}
