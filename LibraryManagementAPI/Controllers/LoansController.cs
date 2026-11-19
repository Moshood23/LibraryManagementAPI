using System.Security.Claims;
using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Repositories;
using LibraryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loans;

    public LoansController(ILoanService loans) => _loans = loans;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>Lists the signed-in user's loans. Set includeReturned=true to include past loans.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LoanReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanReadDto>>>> GetMyLoans([FromQuery] bool includeReturned = false)
    {
        var loans = await _loans.GetForUserAsync(UserId, includeReturned);
        return Ok(ApiResponse<IReadOnlyList<LoanReadDto>>.SuccessResponse(loans));
    }

    /// <summary>Lists all loans (admin). Filter with status=Active, Overdue, Returned or All.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LoanReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanReadDto>>>> GetLoans([FromQuery] LoanStatusFilter status = LoanStatusFilter.Active)
    {
        var loans = await _loans.GetByStatusAsync(status);
        return Ok(ApiResponse<IReadOnlyList<LoanReadDto>>.SuccessResponse(loans));
    }

    /// <summary>Lists loans past their due date (admin).</summary>
    [HttpGet("overdue")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LoanReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanReadDto>>>> GetOverdueLoans()
    {
        var loans = await _loans.GetByStatusAsync(LoanStatusFilter.Overdue);
        return Ok(ApiResponse<IReadOnlyList<LoanReadDto>>.SuccessResponse(loans));
    }

    /// <summary>Gets one loan. Users can only see their own; admins can see any.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LoanReadDto>>> GetLoan(Guid id)
    {
        var result = await _loans.GetAsync(id, UserId, IsAdmin);
        if (!result.Succeeded) return this.ToErrorResult(result);
        return Ok(ApiResponse<LoanReadDto>.SuccessResponse(result.Value));
    }

    /// <summary>Returns a borrowed book. Users can return their own loans; admins can return any.</summary>
    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanReadDto>>> ReturnLoan(Guid id)
    {
        var result = await _loans.ReturnAsync(id, UserId, IsAdmin);
        if (!result.Succeeded) return this.ToErrorResult(result);
        return Ok(ApiResponse<LoanReadDto>.SuccessResponse(result.Value, result.Message));
    }
}
