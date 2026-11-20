using LibraryManagementAPI.DTOs;

namespace LibraryManagementAPI.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterDto model);
    Task<AuthResult> LoginAsync(LoginDto model);

    /// <summary>Swaps a refresh token for a new access token and a new refresh token.</summary>
    Task<AuthResult> RefreshAsync(string refreshToken);

    /// <summary>Revokes a refresh token. Returns false if it was unknown or already revoked.</summary>
    Task<bool> LogoutAsync(string refreshToken);

    /// <summary>Revokes every active refresh token of a user, e.g. when an admin locks the account.</summary>
    Task RevokeAllAsync(string userId);
}
