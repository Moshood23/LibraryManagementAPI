
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class BooksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public BooksController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookReadDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<BookReadDto>>>> GetBooks()
        {
            var books = await _unitOfWork.Books.GetAllWithRelationsAsync();
            var bookDtos = books.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<BookReadDto>>.SuccessResponse(bookDtos));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<BookReadDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BookReadDto>), 404)]
        public async Task<ActionResult<ApiResponse<BookReadDto>>> GetBook(Guid id)
        {
            var book = await _unitOfWork.Books.GetByIdWithRelationsAsync(id);
            if (book == null)
                return NotFound(ApiResponse<BookReadDto>.FailureResponse("Book not found"));

            return Ok(ApiResponse<BookReadDto>.SuccessResponse(MapToDto(book)));
        }

        private BookReadDto MapToDto(Book book)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<BookReadDto>), 201)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<BookReadDto>>> CreateBook(BookCreateUpdateDto bookDto)
        {
            var book = new Book
            {
                Title = bookDto.Title,
                ISBN = bookDto.ISBN,
                PublicationYear = bookDto.PublicationYear,
                AuthorId = bookDto.AuthorId,
                GenreId = bookDto.GenreId
            };

            await _unitOfWork.Books.AddAsync(book);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Books.GetByIdWithRelationsAsync(book.Id);
            return CreatedAtAction(nameof(GetBook), new { id = book.Id },
                ApiResponse<BookReadDto>.SuccessResponse(MapToDto(created!), "Book created successfully"));
        }


        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateBook(Guid id, BookCreateUpdateDto bookDto)
        {
            var book = await _unitOfWork.Books.GetByIdAsync(id);
            if (book == null)
                return NotFound(ApiResponse<object>.FailureResponse("Book not found"));

            book.Title = bookDto.Title;
            book.ISBN = bookDto.ISBN;
            book.PublicationYear = bookDto.PublicationYear;
            book.AuthorId = bookDto.AuthorId;
            book.GenreId = bookDto.GenreId;

            await _unitOfWork.Books.UpdateAsync(book);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Book updated successfully"));
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBook(Guid id)
        {
            if (!await _unitOfWork.Books.ExistsAsync(id))
                return NotFound(ApiResponse<object>.FailureResponse("Book not found"));

            await _unitOfWork.Books.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Book deleted successfully"));
        }

       
    }

}