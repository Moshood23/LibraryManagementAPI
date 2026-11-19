namespace LibraryManagementAPI.DTOs;

public class LoanReadDto
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookISBN { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    /// <summary>"Active", "Overdue" or "Returned".</summary>
    public string Status { get; set; } = string.Empty;
    public int DaysOverdue { get; set; }
}
