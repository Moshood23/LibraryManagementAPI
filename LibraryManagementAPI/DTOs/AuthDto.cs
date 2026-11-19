using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.DTOs;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class AuthResult
{
    public bool Succeeded { get; init; }
    public AuthResponseDto? Response { get; init; }
    public List<string> Errors { get; init; } = new();

    public static AuthResult Success(AuthResponseDto response) => new() { Succeeded = true, Response = response };
    public static AuthResult Failure(IEnumerable<string> errors) => new() { Errors = errors.ToList() };
}
