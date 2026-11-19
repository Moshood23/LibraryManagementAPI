using Microsoft.AspNetCore.Identity;

namespace LibraryManagementAPI.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
