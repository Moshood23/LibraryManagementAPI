
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.DTOs
{
    public class AuthDto
    {
        public class RegisterDto
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            [Required]
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
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new();
        }
    }
}

