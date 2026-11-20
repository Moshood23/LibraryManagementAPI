using System.Security.Claims;
using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using LibraryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

/// <summary>User administration. Admins only.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = Roles.Admin)]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;

    public UsersController(IUserAdminService users) => _users = users;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Lists users one page at a time, optionally filtered by email or name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserReadDto>>>> GetUsers([FromQuery] UserQueryParameters query) =>
        Ok(ApiResponse<PagedResult<UserReadDto>>.SuccessResponse(await _users.GetUsersAsync(query)));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserReadDto>>> GetUser(string id) =>
        Respond(await _users.GetUserAsync(id));

    /// <summary>Replaces a user's roles, e.g. ["User", "Admin"] to promote. You can't remove your own Admin role or the last Admin.</summary>
    [HttpPut("{id}/roles")]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserReadDto>>> SetRoles(string id, UpdateUserRolesDto model) =>
        Respond(await _users.SetRolesAsync(id, model.Roles, CurrentUserId));

    /// <summary>Locks an account: the user can't log in or refresh. You can't lock yourself.</summary>
    [HttpPost("{id}/lock")]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserReadDto>>> Lock(string id) =>
        Respond(await _users.LockAsync(id, CurrentUserId));

    /// <summary>Unlocks an account and clears its failed-login count.</summary>
    [HttpPost("{id}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserReadDto>>> Unlock(string id) =>
        Respond(await _users.UnlockAsync(id));

    private ActionResult<ApiResponse<UserReadDto>> Respond(ServiceResult<UserReadDto> result) =>
        result.Succeeded
            ? Ok(ApiResponse<UserReadDto>.SuccessResponse(result.Value, result.Message))
            : this.ToErrorResult(result);
}
