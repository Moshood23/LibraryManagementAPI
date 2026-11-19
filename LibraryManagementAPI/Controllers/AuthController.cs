using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Registers a new user with the "User" role and returns a JWT.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterDto model)
    {
        var result = await _authService.RegisterAsync(model);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse("Registration failed", result.Errors));

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result.Response, "User registered successfully"));
    }

    /// <summary>Signs in with email and password and returns a JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginDto model)
    {
        var result = await _authService.LoginAsync(model);
        if (result is null)
            return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse("Invalid email or password"));

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful"));
    }
}
