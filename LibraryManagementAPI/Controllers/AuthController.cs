
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Repository;
using LibraryManagementAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static LibraryManagementAPI.DTOs.AuthDto;

namespace LibraryManagementAPI.Controllers
{
    
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterDto model)
        {
            var result = await _authService.RegisterAsync(model);

            if (result == null)
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse("Registration failed"));

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "User registered successfully"));
        }

       
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 401)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginDto model)
        {
            var result = await _authService.LoginAsync(model);

            if (result == null)
                return Unauthorized(ApiResponse<AuthResponseDto>.FailureResponse("Invalid credentials"));

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful"));
        }
    }
}