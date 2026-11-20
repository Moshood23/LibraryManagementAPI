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
public class GenresController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public GenresController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// Lists genres one page at a time. Optional search; sort with sortBy (Name, BookCount, Newest)
    /// and sortDirection (Asc, Desc).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<GenreReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<GenreReadDto>>>> GetGenres([FromQuery] GenreQueryParameters query)
    {
        var (genres, total) = await _unitOfWork.Genres.GetPagedAsync(query);
        var result = PagedResult<GenreReadDto>.Create(genres.Select(g => g.ToReadDto()), query.Page, query.PageSize, total);
        return Ok(ApiResponse<PagedResult<GenreReadDto>>.SuccessResponse(result));
    }

    /// <summary>Searches genres by name or description.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<GenreReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<GenreReadDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<GenreReadDto>>>> SearchGenres([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(ApiResponse<IEnumerable<GenreReadDto>>.FailureResponse("A search term is required"));

        var genres = await _unitOfWork.Genres.SearchGenresAsync(term.Trim());
        return Ok(ApiResponse<IEnumerable<GenreReadDto>>.SuccessResponse(genres.Select(g => g.ToReadDto()).ToList()));
    }

    /// <summary>Gets the genres with the most books.</summary>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<GenreReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<GenreReadDto>>>> GetPopularGenres([FromQuery] int top = 5)
    {
        var genres = await _unitOfWork.Genres.GetPopularGenresAsync(Math.Clamp(top, 1, 50));
        return Ok(ApiResponse<IEnumerable<GenreReadDto>>.SuccessResponse(genres.Select(g => g.ToReadDto()).ToList()));
    }

    /// <summary>Gets one genre together with its books.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GenreWithBooksDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenreWithBooksDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GenreWithBooksDto>>> GetGenre(Guid id)
    {
        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(id);
        if (genre is null)
            return NotFound(ApiResponse<GenreWithBooksDto>.FailureResponse("Genre not found"));

        return Ok(ApiResponse<GenreWithBooksDto>.SuccessResponse(genre.ToWithBooksDto()));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<GenreReadDto>>> CreateGenre(GenreCreateUpdateDto genreDto)
    {
        if (await _unitOfWork.Genres.NameExistsAsync(genreDto.Name))
            return Conflict(ApiResponse<GenreReadDto>.FailureResponse("A genre with this name already exists"));

        var genre = new Genre { Name = genreDto.Name.Trim(), Description = genreDto.Description };

        await _unitOfWork.Genres.AddAsync(genre);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGenre), new { id = genre.Id, version = "1.0" },
            ApiResponse<GenreReadDto>.SuccessResponse(genre.ToReadDto(), "Genre created successfully"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<GenreReadDto>>> UpdateGenre(Guid id, GenreCreateUpdateDto genreDto)
    {
        var genre = await _unitOfWork.Genres.GetByIdWithBooksAsync(id);
        if (genre is null)
            return NotFound(ApiResponse<GenreReadDto>.FailureResponse("Genre not found"));

        if (await _unitOfWork.Genres.NameExistsAsync(genreDto.Name, id))
            return Conflict(ApiResponse<GenreReadDto>.FailureResponse("A genre with this name already exists"));

        genre.Name = genreDto.Name.Trim();
        genre.Description = genreDto.Description;
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<GenreReadDto>.SuccessResponse(genre.ToReadDto(), "Genre updated successfully"));
    }

    /// <summary>Soft-deletes a genre. Fails with 409 while the genre still has books.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteGenre(Guid id)
    {
        if (!await _unitOfWork.Genres.ExistsAsync(id))
            return NotFound(ApiResponse<object>.FailureResponse("Genre not found"));

        if (await _unitOfWork.Genres.HasBooksAsync(id))
            return Conflict(ApiResponse<object>.FailureResponse("Genre still has books. Delete or reassign them first."));

        await _unitOfWork.Genres.SoftDeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(null, "Genre deleted successfully"));
    }
}
