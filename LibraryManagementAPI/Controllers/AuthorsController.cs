using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AuthorsController : ControllerBase
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly IBookRepository _bookRepository;

        public AuthorsController(IAuthorRepository authorRepository, IBookRepository bookRepository)
        {
            _authorRepository = authorRepository;
            _bookRepository = bookRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuthorReadDto>>> GetAuthors()
        {
            var authors = await _authorRepository.GetAllAsync();
            var authorDtos = authors.Select(a => MapToReadDto(a));
            return Ok(authorDtos);
        }

        private object MapToReadDto(Author a)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuthorReadDto>> GetAuthor(Guid id)
        {
            var author = await _authorRepository.GetByIdAsync(id);
            if (author == null)
            {
                return NotFound(new { message = $"Author with ID {id} not found" });
            }

            return Ok(MapToReadDto(author));
        }


        [HttpPost]
        public async Task<ActionResult<AuthorReadDto>> CreateAuthor(AuthorCreateUpdateDto authorDto)
        {
            var author = new Author
            {
                FirstName = authorDto.FirstName,
                LastName = authorDto.LastName,
                Bio = authorDto.Bio,
                DateOfBirth = authorDto.DateOfBirth
            };

            var createdAuthor = await _authorRepository.AddAsync(author);
            var fullAuthor = await _authorRepository.GetByIdAsync(createdAuthor.Id);

            return CreatedAtAction(nameof(GetAuthor), new { id = fullAuthor!.Id }, MapToReadDto(fullAuthor));
        }

      

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(Guid id)
        {
            var deleted = await _authorRepository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Author with ID {id} not found" });
            }

            return NoContent();
        }


        private static AuthorReadDto FromAuthor(Author author)
        {
            return new AuthorReadDto
            {
                Id = author.Id,
                FirstName = author.FirstName,
                LastName = author.LastName,
                Bio = author.Bio,
                DateOfBirth = author.DateOfBirth,
                BookCount = author.Books?.Count ?? 0
            };
        }
    }
}