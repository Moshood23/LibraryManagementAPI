using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models;

/// <summary>Lending rules, bound from the "Loans" configuration section.</summary>
public class LoanOptions
{
    public const string SectionName = "Loans";

    /// <summary>How many days a user may keep a book.</summary>
    [Range(1, 365)]
    public int LoanPeriodDays { get; set; } = 14;

    /// <summary>How many books one user may have out at the same time.</summary>
    [Range(1, 100)]
    public int MaxActiveLoansPerUser { get; set; } = 5;
}
