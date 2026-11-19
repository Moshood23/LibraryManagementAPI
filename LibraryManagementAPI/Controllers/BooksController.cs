using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Repositories;
using LibraryManagementAPI.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BooksController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly IUnitOfWork _unitOfWork;

    public BooksController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>Lists books, one page at a time.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookReadDto>>>> GetBooks(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (books, total) = await _unitOfWork.Books.GetPagedBooksAsync(page, pageSize);
        var result = new PagedResult<BookReadDto>
        {
            Items = books.Select(b => b.ToReadDto()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
        return Ok(ApiResponse<PagedResult<BookReadDto>>.SuccessResponse(result));
    }

    /// <summary>Searches books by title, ISBN, description, author or genre.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookReadDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookReadDto>>>> SearchBooks([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(ApiResponse<IEnumerable<BookReadDto>>.FailureResponse("A search term is required"));

        var books = await _unitOfWork.Books.SearchBooksAsync(term.Trim());
        return Ok(ApiResponse<IEnumerable<BookReadDto>>.SuccessResponse(books.Select(b => b.ToReadDto()).ToList()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookReadDto>>> GetBook(Guid id)
    {
        var book = await _unitOfWork.Books.GetByIdWithRelationsAsync(id);
        if (book is null)
            return NotFound(ApiResponse<BookReadDto>.FailureResponse("Book not found"));

        return Ok(ApiResponse<BookReadDto>.SuccessResponse(book.ToReadDto()));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<BookReadDto>>> CreateBook(BookCreateUpdateDto bookDto)
    {
        var errors = await ValidateRelationsAsync(bookDto);
        if (errors.Count > 0)
            return BadRequest(ApiResponse<BookReadDto>.FailureResponse("Invalid book", errors));

        if (await _unitOfWork.Books.IsbnExistsAsync(Isbn.Normalize(bookDto.ISBN)))
            return Conflict(ApiResponse<BookReadDto>.FailureResponse("A book with this ISBN already exists"));

        var book = new Book();
        Apply(bookDto, book);

        await _unitOfWork.Books.AddAsync(book);
        await _unitOfWork.SaveChangesAsync();

        var created = await _unitOfWork.Books.GetByIdWithRelationsAsync(book.Id);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id, version = "1.0" },
            ApiResponse<BookReadDto>.SuccessResponse(created!.ToReadDto(), "Book created successfully"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BookReadDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<BookReadDto>>> UpdateBook(Guid id, BookCreateUpdateDto bookDto)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(id);
        if (book is null)
            return NotFound(ApiResponse<BookReadDto>.FailureResponse("Book not found"));

        var errors = await ValidateRelationsAsync(bookDto);
        if (errors.Count > 0)
            return BadRequest(ApiResponse<BookReadDto>.FailureResponse("Invalid book", errors));

        if (await _unitOfWork.Books.IsbnExistsAsync(Isbn.Normalize(bookDto.ISBN), id))
            return Conflict(ApiResponse<BookReadDto>.FailureResponse("A book with this ISBN already exists"));

        Apply(bookDto, book);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.Books.GetByIdWithRelationsAsync(id);
        return Ok(ApiResponse<BookReadDto>.SuccessResponse(updated!.ToReadDto(), "Book updated successfully"));
    }

    /// <summary>Soft-deletes a book.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBook(Guid id)
    {
        if (!await _unitOfWork.Books.SoftDeleteAsync(id))
            return NotFound(ApiResponse<object>.FailureResponse("Book not found"));

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(null, "Book deleted successfully"));
    }

    private async Task<List<string>> ValidateRelationsAsync(BookCreateUpdateDto dto)
    {
        var errors = new List<string>();
        if (!await _unitOfWork.Authors.ExistsAsync(dto.AuthorId))
            errors.Add($"Author '{dto.AuthorId}' does not exist.");
        if (!await _unitOfWork.Genres.ExistsAsync(dto.GenreId))
            errors.Add($"Genre '{dto.GenreId}' does not exist.");
        return errors;
    }

    private static void Apply(BookCreateUpdateDto dto, Book book)
    {
        book.Title = dto.Title.Trim();
        book.ISBN = Isbn.Normalize(dto.ISBN);
        book.PublicationYear = dto.PublicationYear;
        book.Description = dto.Description;
        book.AuthorId = dto.AuthorId;
        book.GenreId = dto.GenreId;
    }
}
