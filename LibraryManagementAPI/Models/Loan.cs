namespace LibraryManagementAPI.Models;

/// <summary>One copy of a book lent to one user.</summary>
public class Loan : BaseEntity
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }

    /// <summary>Null while the book is still out.</summary>
    public DateTime? ReturnedAt { get; set; }

    public bool IsActive => ReturnedAt is null;
    public bool IsOverdue(DateTime utcNow) => ReturnedAt is null && DueAt < utcNow;
}
