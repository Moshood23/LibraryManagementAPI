using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Queries;

public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>Page number and size shared by every list endpoint.</summary>
public class PagingParameters
{
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Items per page (1–100).</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 20;
}

public enum BookSortBy
{
    Title,
    Year,
    Author,
    Newest
}

/// <summary>Filters and sort order for GET /books. Every filter is optional and they combine with AND.</summary>
public class BookQueryParameters : PagingParameters, IValidatableObject
{
    /// <summary>Matches title, ISBN, description, author name or genre name.</summary>
    [StringLength(100)]
    public string? Search { get; set; }

    public Guid? AuthorId { get; set; }
    public Guid? GenreId { get; set; }

    /// <summary>Earliest publication year (inclusive).</summary>
    [Range(1000, 2100)]
    public int? YearFrom { get; set; }

    /// <summary>Latest publication year (inclusive).</summary>
    [Range(1000, 2100)]
    public int? YearTo { get; set; }

    /// <summary>true = only books with a copy on the shelf; false = only books with none.</summary>
    public bool? Available { get; set; }

    public BookSortBy SortBy { get; set; } = BookSortBy.Title;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (YearFrom is not null && YearTo is not null && YearFrom > YearTo)
            yield return new ValidationResult("YearFrom can't be later than YearTo.", new[] { nameof(YearFrom) });

        foreach (var error in EnumChecks.Validate(SortBy, nameof(SortBy), SortDirection))
            yield return error;
    }
}

public enum AuthorSortBy
{
    LastName,
    FirstName,
    BookCount,
    Newest
}

public class AuthorQueryParameters : PagingParameters, IValidatableObject
{
    /// <summary>Matches first name, last name or bio.</summary>
    [StringLength(100)]
    public string? Search { get; set; }

    public AuthorSortBy SortBy { get; set; } = AuthorSortBy.LastName;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        EnumChecks.Validate(SortBy, nameof(SortBy), SortDirection);
}

public enum GenreSortBy
{
    Name,
    BookCount,
    Newest
}

public class GenreQueryParameters : PagingParameters, IValidatableObject
{
    /// <summary>Matches name or description.</summary>
    [StringLength(100)]
    public string? Search { get; set; }

    public GenreSortBy SortBy { get; set; } = GenreSortBy.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        EnumChecks.Validate(SortBy, nameof(SortBy), SortDirection);
}

public class UserQueryParameters : PagingParameters
{
    /// <summary>Matches email or full name.</summary>
    [StringLength(100)]
    public string? Search { get; set; }
}

internal static class EnumChecks
{
    /// <summary>Query binding accepts any number for an enum (e.g. sortBy=42); reject values that aren't defined.</summary>
    public static IEnumerable<ValidationResult> Validate<TSort>(TSort sortBy, string sortName, SortDirection direction)
        where TSort : struct, Enum
    {
        if (!Enum.IsDefined(sortBy))
            yield return new ValidationResult(
                $"{sortName} must be one of: {string.Join(", ", Enum.GetNames<TSort>())}.", new[] { sortName });
        if (!Enum.IsDefined(direction))
            yield return new ValidationResult("SortDirection must be Asc or Desc.", new[] { nameof(SortDirection) });
    }
}
