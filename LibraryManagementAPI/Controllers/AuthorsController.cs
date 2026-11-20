using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AuthorsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// Lists authors one page at a time. Optional search; sort with sortBy (LastName, FirstName, BookCount, Newest)
    /// and sortDirection (Asc, Desc).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuthorReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuthorReadDto>>>> GetAuthors([FromQuery] AuthorQueryParameters query)
    {
        var (authors, total) = await _unitOfWork.Authors.GetPagedAsync(query);
        var result = PagedResult<AuthorReadDto>.Create(authors.Select(a => a.ToReadDto()), query.Page, query.PageSize, total);
        return Ok(ApiResponse<PagedResult<AuthorReadDto>>.SuccessResponse(result));
    }

    /// <summary>Searches authors by first name, last name or bio.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuthorReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuthorReadDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AuthorReadDto>>>> SearchAuthors([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(ApiResponse<IEnumerable<AuthorReadDto>>.FailureResponse("A search term is required"));

        var authors = await _unitOfWork.Authors.SearchAuthorsAsync(term.Trim());
        return Ok(ApiResponse<IEnumerable<AuthorReadDto>>.SuccessResponse(authors.Select(a => a.ToReadDto()).ToList()));
    }

    /// <summary>Gets one author together with their books.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuthorWithBooksDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthorWithBooksDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuthorWithBooksDto>>> GetAuthor(Guid id)
    {
        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(id);
        if (author is null)
            return NotFound(ApiResponse<AuthorWithBooksDto>.FailureResponse("Author not found"));

        return Ok(ApiResponse<AuthorWithBooksDto>.SuccessResponse(author.ToWithBooksDto()));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AuthorReadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthorReadDto>>> CreateAuthor(AuthorCreateUpdateDto authorDto)
    {
        var author = new Author();
        Apply(authorDto, author);

        await _unitOfWork.Authors.AddAsync(author);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAuthor), new { id = author.Id, version = "1.0" },
            ApiResponse<AuthorReadDto>.SuccessResponse(author.ToReadDto(), "Author created successfully"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AuthorReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthorReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthorReadDto>>> UpdateAuthor(Guid id, AuthorCreateUpdateDto authorDto)
    {
        var author = await _unitOfWork.Authors.GetByIdWithBooksAsync(id);
        if (author is null)
            return NotFound(ApiResponse<AuthorReadDto>.FailureResponse("Author not found"));

        Apply(authorDto, author);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<AuthorReadDto>.SuccessResponse(author.ToReadDto(), "Author updated successfully"));
    }

    /// <summary>Soft-deletes an author. Fails with 409 while the author still has books.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAuthor(Guid id)
    {
        if (!await _unitOfWork.Authors.ExistsAsync(id))
            return NotFound(ApiResponse<object>.FailureResponse("Author not found"));

        if (await _unitOfWork.Authors.HasBooksAsync(id))
            return Conflict(ApiResponse<object>.FailureResponse("Author still has books. Delete or reassign them first."));

        await _unitOfWork.Authors.SoftDeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(null, "Author deleted successfully"));
    }

    private static void Apply(AuthorCreateUpdateDto dto, Author author)
    {
        author.FirstName = dto.FirstName.Trim();
        author.LastName = dto.LastName.Trim();
        author.Bio = dto.Bio;
        author.DateOfBirth = dto.DateOfBirth;
    }
}
