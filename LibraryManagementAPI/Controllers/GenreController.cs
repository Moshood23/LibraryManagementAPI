using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly IGenreRepository _genreRepository;
        private readonly IBookRepository _bookRepository;

        public GenresController(IGenreRepository genreRepository, IBookRepository bookRepository)
        {
            _genreRepository = genreRepository;
            _bookRepository = bookRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GenreReadDto>>> GetGenres()
        {
            var genres = await _genreRepository.GetAllAsync();
            var genreDtos = genres.Select(g => MapToReadDto(g));
            return Ok(genreDtos);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<GenreReadDto>> GetGenre(int id)
        {
            var genre = await _genreRepository.GetByIdAsync(id);
            if (genre == null)
            {
                return NotFound(new { message = $"Genre with ID {id} not found" });
            }

            return Ok(MapToReadDto(genre));
        }



        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<GenreReadDto>>> SearchGenres([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            var genres = await _genreRepository.SearchAsync(term);
            var genreDtos = genres.Select(g => MapToReadDto(g));
            return Ok(genreDtos);
        }

        [HttpGet("popular")]
        public async Task<ActionResult<IEnumerable<GenreReadDto>>> GetPopularGenres([FromQuery] int count = 5)
        {
            if (count < 1)
            {
                return BadRequest(new { message = "Count must be greater than 0" });
            }

            var genres = await _genreRepository.GetPopularGenresAsync(count);
            var genreDtos = genres.Select(g => MapToReadDto(g));
            return Ok(genreDtos);
        }

        [HttpGet("with-books")]
        public async Task<ActionResult<IEnumerable<GenreReadDto>>> GetGenresWithBooks()
        {
            var genres = await _genreRepository.GetGenresWithBooksAsync();
            var genreDtos = genres.Select(g => MapToReadDto(g));
            return Ok(genreDtos);
        }

        [HttpGet("{id}/book-count")]
        public async Task<ActionResult<object>> GetGenreBookCount(int id)
        {
            if (!await _genreRepository.ExistsAsync(id))
            {
                return NotFound(new { message = $"Genre with ID {id} not found" });
            }

            var count = await _genreRepository.GetBookCountByGenreAsync(id);
            return Ok(new { genreId = id, bookCount = count });
        }


        [HttpPost]
        public async Task<ActionResult<GenreReadDto>> CreateGenre(GenreCreateUpdateDto genreDto)
        {
            var genre = new Genre
            {
                Name = genreDto.Name,
                Description = genreDto.Description
            };

            var createdGenre = await _genreRepository.AddAsync(genre);
            var fullGenre = await _genreRepository.GetByIdAsync(createdGenre.Id);

            return CreatedAtAction(nameof(GetGenre), new { id = fullGenre!.Id }, MapToReadDto(fullGenre));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGenre(int id, GenreCreateUpdateDto genreDto)
        {
            if (!await _genreRepository.ExistsAsync(id))
            {
                return NotFound(new { message = $"Genre with ID {id} not found" });
            }

            var existingGenre = await _genreRepository.GetByIdAsync(id);
            if (existingGenre == null)
            {
                return NotFound(new { message = $"Genre with ID {id} not found" });
            }

            existingGenre.Name = genreDto.Name;
            existingGenre.Description = genreDto.Description;

            await _genreRepository.UpdateAsync(existingGenre);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var deleted = await _genreRepository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Genre with ID {id} not found" });
            }

            return NoContent();
        }

        [HttpDelete("{id}/soft")]
        public async Task<IActionResult> SoftDeleteGenre(int id)
        {
            if (!await _genreRepository.ExistsAsync(id))
            {
                return NotFound(new { message = $"Genre with ID {id} not found" });
            }

            await _genreRepository.SoftDeleteAsync(id);
            return NoContent();
        }

        private static GenreReadDto MapToReadDto(Genre genre)
        {
            return new GenreReadDto
            {
                Id = genre.Id,
                Name = genre.Name,
                Description = genre.Description,
                BookCount = genre.Books?.Count ?? 0,
                CreatedAt = genre.CreatedAt,
                UpdatedAt = genre.UpdatedAt
            };
        }

    }
}