using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(RateLimitingOptions.AuthPolicy)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Registers a new user with the "User" role and returns an access token and a refresh token.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterDto model)
    {
        var result = await _authService.RegisterAsync(model);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse("Registration failed", result.Errors));

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result.Response, "User registered successfully"));
    }

    /// <summary>Signs in with email and password. Five wrong passwords in a row lock the account for 15 minutes.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginDto model)
    {
        var result = await _authService.LoginAsync(model);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(result.Errors.First()));

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result.Response, "Login successful"));
    }

    /// <summary>
    /// Swaps a refresh token for a new access token and a new refresh token. Each refresh token works once;
    /// reusing an old one signs the user out everywhere.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh(RefreshTokenRequestDto model)
    {
        var result = await _authService.RefreshAsync(model.RefreshToken);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse(result.Errors.First()));

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result.Response, "Token refreshed"));
    }

    /// <summary>Signs out by revoking the given refresh token.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Logout(RefreshTokenRequestDto model)
    {
        // Same answer whether or not the token existed, so this can't be used to test tokens.
        await _authService.LogoutAsync(model.RefreshToken);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Logged out"));
    }
}
