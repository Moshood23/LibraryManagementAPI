using Microsoft.AspNetCore.Identity;

namespace LibraryManagementAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public required string Email     { get; set; } 
        public string Password { get; set; }

    }
}