using LibraryManagementAPI.DTOs;
using static LibraryManagementAPI.DTOs.AuthDto;

namespace LibraryManagementAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto model);
        Task<AuthResponseDto?> LoginAsync(LoginDto model);
    }
}