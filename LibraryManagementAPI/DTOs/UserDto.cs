using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.DTOs;

public class UserReadDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}

public class UpdateUserRolesDto
{
    /// <summary>The complete set of roles the user should have, e.g. ["User"] or ["User", "Admin"].</summary>
    [Required]
    [MinLength(1, ErrorMessage = "A user needs at least one role.")]
    public List<string> Roles { get; set; } = new();
}
