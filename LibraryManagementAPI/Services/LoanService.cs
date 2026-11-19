using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagementAPI.Services;

public class LoanService : ILoanService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;
    private readonly LoanOptions _options;

    public LoanService(IUnitOfWork unitOfWork, TimeProvider time, IOptions<LoanOptions> options)
    {
        _unitOfWork = unitOfWork;
        _time = time;
        _options = options.Value;
    }

    private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    public async Task<ServiceResult<LoanReadDto>> BorrowAsync(Guid bookId, string userId)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var book = await _unitOfWork.Books.GetByIdAsync(bookId);
            if (book is null)
                return Fail(ServiceError.NotFound, "Book not found");

            if (await _unitOfWork.Loans.HasActiveLoanAsync(userId, bookId))
                return Fail(ServiceError.Conflict, "You already have this book on loan.");

            if (await _unitOfWork.Loans.HasOverdueLoansAsync(userId, UtcNow))
                return Fail(ServiceError.Conflict, "You have overdue books. Return them before borrowing more.");

            if (await _unitOfWork.Loans.CountActiveForUserAsync(userId) >= _options.MaxActiveLoansPerUser)
                return Fail(ServiceError.Conflict,
                    $"You can't have more than {_options.MaxActiveLoansPerUser} books on loan at once.");

            if (book.AvailableCopies <= 0)
                return Fail(ServiceError.Conflict, "No copies of this book are available right now.");

            var now = UtcNow;
            var loan = new Loan
            {
                BookId = book.Id,
                UserId = userId,
                BorrowedAt = now,
                DueAt = now.AddDays(_options.LoanPeriodDays)
            };
            book.AvailableCopies--;
            await _unitOfWork.Loans.AddAsync(loan);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Someone else changed this book's copies at the same time; start again with fresh data.
                _unitOfWork.ClearChanges();
                continue;
            }

            return await LoadAsync(loan.Id, "Book borrowed successfully");
        }

        return Fail(ServiceError.Conflict, "The book is in high demand right now. Please try again.");
    }

    public async Task<ServiceResult<LoanReadDto>> ReturnAsync(Guid loanId, string userId, bool isAdmin)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var loan = await _unitOfWork.Loans.GetByIdWithDetailsAsync(loanId);
            if (loan is null || (!isAdmin && loan.UserId != userId))
                return Fail(ServiceError.NotFound, "Loan not found");

            if (!loan.IsActive)
                return Fail(ServiceError.Conflict, "This book has already been returned.");

            loan.ReturnedAt = UtcNow;
            loan.Book.AvailableCopies = Math.Min(loan.Book.AvailableCopies + 1, loan.Book.TotalCopies);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _unitOfWork.ClearChanges();
                continue;
            }

            return ServiceResult<LoanReadDto>.Ok(ToDto(loan), "Book returned successfully");
        }

        return Fail(ServiceError.Conflict, "Could not record the return because the book was being updated. Please try again.");
    }

    public async Task<ServiceResult<LoanReadDto>> GetAsync(Guid loanId, string userId, bool isAdmin)
    {
        var loan = await _unitOfWork.Loans.GetByIdWithDetailsAsync(loanId);
        // Hide other people's loans behind 404 so IDs can't be probed.
        if (loan is null || (!isAdmin && loan.UserId != userId))
            return Fail(ServiceError.NotFound, "Loan not found");

        return ServiceResult<LoanReadDto>.Ok(ToDto(loan));
    }

    public async Task<IReadOnlyList<LoanReadDto>> GetForUserAsync(string userId, bool includeReturned) =>
        (await _unitOfWork.Loans.GetForUserAsync(userId, includeReturned)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<LoanReadDto>> GetByStatusAsync(LoanStatusFilter status) =>
        (await _unitOfWork.Loans.GetByStatusAsync(status, UtcNow)).Select(ToDto).ToList();

    private async Task<ServiceResult<LoanReadDto>> LoadAsync(Guid loanId, string message)
    {
        var loan = await _unitOfWork.Loans.GetByIdWithDetailsAsync(loanId);
        return ServiceResult<LoanReadDto>.Ok(ToDto(loan!), message);
    }

    private static ServiceResult<LoanReadDto> Fail(ServiceError error, string message) =>
        ServiceResult<LoanReadDto>.Fail(error, message);

    private LoanReadDto ToDto(Loan loan)
    {
        var now = UtcNow;
        var overdue = loan.IsOverdue(now);
        return new LoanReadDto
        {
            Id = loan.Id,
            BookId = loan.BookId,
            BookTitle = loan.Book?.Title ?? string.Empty,
            BookISBN = loan.Book?.ISBN ?? string.Empty,
            UserId = loan.UserId,
            UserEmail = loan.User?.Email ?? string.Empty,
            UserFullName = loan.User?.FullName ?? string.Empty,
            BorrowedAt = loan.BorrowedAt,
            DueAt = loan.DueAt,
            ReturnedAt = loan.ReturnedAt,
            Status = loan.ReturnedAt is not null ? "Returned" : overdue ? "Overdue" : "Active",
            DaysOverdue = overdue ? (int)Math.Ceiling((now - loan.DueAt).TotalDays) : 0
        };
    }
}
