using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Validation;

/// <summary>Helpers for ISBN-10 and ISBN-13 numbers.</summary>
public static class Isbn
{
    /// <summary>Removes hyphens and spaces and upper-cases a trailing 'x'.</summary>
    public static string Normalize(string? value) =>
        new string((value ?? string.Empty).Where(c => c != '-' && c != ' ').ToArray()).ToUpperInvariant();

    /// <summary>True when the value (after normalising) is a valid ISBN-10 or ISBN-13, including its check digit.</summary>
    public static bool IsValid(string? value)
    {
        var isbn = Normalize(value);
        return isbn.Length switch
        {
            10 => IsValidIsbn10(isbn),
            13 => IsValidIsbn13(isbn),
            _ => false
        };
    }

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int digit;
            if (char.IsDigit(isbn[i])) digit = isbn[i] - '0';
            else if (i == 9 && isbn[i] == 'X') digit = 10;
            else return false;
            sum += digit * (10 - i);
        }
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsDigit)) return false;
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        var check = (10 - sum % 10) % 10;
        return check == isbn[12] - '0';
    }
}

/// <summary>Validates that a string is an ISBN-10 or ISBN-13 (hyphens and spaces allowed).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IsbnAttribute : ValidationAttribute
{
    public IsbnAttribute() : base("{0} must be a valid ISBN-10 or ISBN-13.") { }

    public override bool IsValid(object? value) =>
        value is null || (value is string s && Isbn.IsValid(s));
}
