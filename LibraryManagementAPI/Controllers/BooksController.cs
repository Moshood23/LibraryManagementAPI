using System.Collections;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;

        public BooksController(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable>> GetBooksAsync()
        {
            var books = await _bookRepository.GetAllAsync();
            var bookDtos = books.Select(b => MapToReadDto(b));
            return Ok(bookDtos);
        }

        private object MapToReadDto(Book b)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookReadDto>> GetBook(Guid id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
                return NotFound(new { message = $"Book with ID {id} not found" });

            return Ok(MapToDto(book));
        }
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BookReadDto>>> SearchBooks([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required" });

            var books = await _bookRepository.SearchBooksAsync(term);
            return Ok(books.Select(MapToDto));
        }
        [HttpGet("author/{authorId}")]
        public async Task<ActionResult<IEnumerable<BookReadDto>>> GetBooksByAuthor(Guid authorId)
        {
            var books = await _bookRepository.GetBooksByAuthorAsync(authorId);
            return Ok(books.Select(MapToDto));
        }

        [HttpGet("paged")]
        public async Task<ActionResult> GetPagedBooks([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Page and pageSize must be greater than 0" });

            var (books, totalCount) = await _bookRepository.GetPagedBooksAsync(page, pageSize);

            return Ok(new
            {
                books = books.Select(MapToDto),
                currentPage = page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        [HttpPost]

        public async Task<ActionResult<BookReadDto>> CreateBook(BookCreateUpdateDto bookDto)
        {
            var book = new Book
            {
                Title = bookDto.Title,
                ISBN = bookDto.ISBN,
                PublicationYear = bookDto.PublicationYear,
                AuthorId = bookDto.authorId,
                GenreId = bookDto.genreId
            };

            var created = await _bookRepository.AddAsync(book);
            var fullBook = await _bookRepository.GetByIdAsync(created.Id);

            return CreatedAtAction(nameof(GetBook), new { id = fullBook!.Id }, MapToDto(fullBook));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(Guid id, BookCreateUpdateDto bookDto)
        {
            var existing = await _bookRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Book with ID {id} not found" });

            existing.Title = bookDto.Title;
            existing.ISBN = bookDto.ISBN;
            existing.PublicationYear = bookDto.PublicationYear;
            existing.AuthorId = bookDto.authorId;
            existing.GenreId = bookDto.genreId;

            await _bookRepository.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}/soft")]
        public async Task<IActionResult> SoftDeleteBook(Guid id)
        {
            if (!await _bookRepository.ExistsAsync(id))
                return NotFound(new { message = $"Book with ID {id} not found" });

            await _bookRepository.SoftDeleteAsync(id);
            return NoContent();
        }
        private static BookReadDto MapToDto(Book book)
        {
            return new()
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                PublicationYear = book.PublicationYear,
                AuthorName = $"{book.Author.FirstName} {book.Author.LastName}",
                GenreName = book.Genre.Name



            };
        }
    }
}

    
