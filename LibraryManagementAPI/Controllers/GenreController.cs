
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class GenresController(IUnitOfWork unitOfWork) : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private object? created;
        private object? genre; 

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<GenreReadDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<GenreReadDto>>>> GetGenres()
        {
            var genres = await _unitOfWork.Genres.GetAllWithBooksAsync();
            var genreDtos = genres.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<GenreReadDto>>.SuccessResponse(genreDtos));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), 404)]
        public async Task<ActionResult<ApiResponse<GenreReadDto>>> GetGenre(Guid id)
        {
            await _unitOfWork.Genres.GetByIdWithBooksAsync(id);
            if (genre == null)
                return NotFound(ApiResponse<GenreReadDto>.FailureResponse("Genre not found"));

            return Ok(ApiResponse<GenreReadDto>.SuccessResponse(MapToDto(genre)));
        } 

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<GenreReadDto>), 201)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<GenreReadDto>>> CreateGenre(GenreCreateUpdateDto genreDto)
        {
            var genre = new Genre
            {
                Name = (string)genreDto.Name,
                Description = (string)genreDto.Description
            };

            await _unitOfWork.Genres.AddAsync(genre);
            await _unitOfWork.SaveChangesAsync();

             await _unitOfWork.Genres.GetByIdWithBooksAsync(genre.Id);
            return CreatedAtAction(nameof(GetGenre), new { id = genre.Id },
                ApiResponse<GenreReadDto>.SuccessResponse(MapToDto(created!), "Genre created successfully"));
        }

        private GenreReadDto MapToDto(object value)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateGenre(Guid id, GenreCreateUpdateDto genreDto)
        {
            var genre = await _unitOfWork.Genres.GetByIdAsync(id);
            if (genre == null)
                return NotFound(ApiResponse<object>.FailureResponse("Genre not found"));

            genre.Name = (string)genreDto.Name;
            genre.Description = (string)genreDto.Description;

            await _unitOfWork.Genres.UpdateAsync(genre);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Genre updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteGenre(Guid id)
        {
            if (!await _unitOfWork.Genres.ExistsAsync(id))
                return NotFound(ApiResponse<object>.FailureResponse("Genre not found"));

            await _unitOfWork.Genres.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Genre deleted successfully"));
        }

    }
}
