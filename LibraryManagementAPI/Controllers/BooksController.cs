using Asp.Versioning;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using System.Security.Claims;
using LibraryManagementAPI.Repositories;
using LibraryManagementAPI.Services;
using LibraryManagementAPI.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoanService _loans;

    public BooksController(IUnitOfWork unitOfWork, ILoanService loans)
    {
        _unitOfWork = unitOfWork;
        _loans = loans;
    }

    /// <summary>
    /// Lists books one page at a time. Optional filters (search, authorId, genreId, yearFrom, yearTo, available)
    /// combine with AND; sort with sortBy (Title, Year, Author, Newest) and sortDirection (Asc, Desc).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookReadDto>>>> GetBooks([FromQuery] BookQueryParameters query)
    {
        var (books, total) = await _unitOfWork.Books.GetPagedBooksAsync(query);
        var result = PagedResult<BookReadDto>.Create(books.Select(b => b.ToReadDto()), query.Page, query.PageSize, total);
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
        book.TotalCopies = bookDto.TotalCopies;
        book.AvailableCopies = bookDto.TotalCopies;

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

        var onLoan = book.TotalCopies - book.AvailableCopies;
        if (bookDto.TotalCopies < onLoan)
            return BadRequest(ApiResponse<BookReadDto>.FailureResponse(
                $"{onLoan} copies are on loan, so TotalCopies can't be lower than {onLoan}."));

        Apply(bookDto, book);
        book.TotalCopies = bookDto.TotalCopies;
        book.AvailableCopies = bookDto.TotalCopies - onLoan;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<BookReadDto>.FailureResponse(
                "The book was borrowed or returned while you were editing it. Reload and try again."));
        }

        var updated = await _unitOfWork.Books.GetByIdWithRelationsAsync(id);
        return Ok(ApiResponse<BookReadDto>.SuccessResponse(updated!.ToReadDto(), "Book updated successfully"));
    }

    /// <summary>Soft-deletes a book. Fails with 409 while copies are on loan.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBook(Guid id)
    {
        if (await _unitOfWork.Loans.BookHasActiveLoansAsync(id))
            return Conflict(ApiResponse<object>.FailureResponse("Copies of this book are still on loan. Wait until they are returned."));

        if (!await _unitOfWork.Books.SoftDeleteAsync(id))
            return NotFound(ApiResponse<object>.FailureResponse("Book not found"));

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(null, "Book deleted successfully"));
    }

    /// <summary>Borrows one copy of this book for the signed-in user.</summary>
    [HttpPost("{id:guid}/borrow")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LoanReadDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoanReadDto>>> BorrowBook(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _loans.BorrowAsync(id, userId);
        if (!result.Succeeded) return this.ToErrorResult(result);

        return CreatedAtAction(nameof(LoansController.GetLoan), "Loans", new { id = result.Value!.Id, version = "1.0" },
            ApiResponse<LoanReadDto>.SuccessResponse(result.Value, result.Message));
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
