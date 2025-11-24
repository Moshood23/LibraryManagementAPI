using LibraryManagementAPI.DTOs;
using static LibraryManagementAPI.DTOs.AuthDto;

namespace LibraryManagementAPI.Repository
{
    public interface IAuthRepository
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto model);
        Task<AuthResponseDto?> LoginAsync(LoginDto model);
    }
}