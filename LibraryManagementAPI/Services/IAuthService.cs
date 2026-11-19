using LibraryManagementAPI.DTOs;

namespace LibraryManagementAPI.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterDto model);
    Task<AuthResponseDto?> LoginAsync(LoginDto model);
}
