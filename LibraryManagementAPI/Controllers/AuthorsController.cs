
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
    public class AuthorsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private object author;

        public AuthorsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuthorReadDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<AuthorReadDto>>>> GetAuthors()
        {
            var authors = await _unitOfWork.Authors.GetAllWithBooksAsync();
            var authorDtos = authors.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<AuthorReadDto>>.SuccessResponse(authorDtos));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AuthorReadDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthorReadDto>), 404)]
        public async Task<ActionResult<ApiResponse<AuthorReadDto>>> GetAuthor(Guid id)
        {
             await _unitOfWork.Authors.GetByIdWithBooksAsync(id);
            if (author == null)
                return NotFound(ApiResponse<AuthorReadDto>.FailureResponse("Author not found"));

            return Ok(ApiResponse<AuthorReadDto>.SuccessResponse(MapToDto(author)));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AuthorReadDto>), 201)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<AuthorReadDto>>> CreateAuthor(AuthorCreateUpdateDto authorDto)
        {
            var author = new Author
            {
                FirstName = authorDto.FirstName,
                LastName = authorDto.LastName,
                Bio = authorDto.Bio,
                DateOfBirth = authorDto.DateOfBirth
            };

            await _unitOfWork.Authors.AddAsync(author);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.Authors.GetByIdWithBooksAsync(author.Id);
            object created = null;
            return CreatedAtAction(nameof(GetAuthor), new { id = author.Id },
                ApiResponse<AuthorReadDto>.SuccessResponse(MapToDto(created!), "Author created successfully"));
        }

        private AuthorReadDto MapToDto(object value)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateAuthor(Guid id, AuthorCreateUpdateDto authorDto)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(id);
            if (author == null)
                return NotFound(ApiResponse<object>.FailureResponse("Author not found"));

            author.FirstName = authorDto.FirstName;
            author.LastName = authorDto.LastName;
            author.Bio = authorDto.Bio;
            author.DateOfBirth = authorDto.DateOfBirth;

            await _unitOfWork.Authors.UpdateAsync(author);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Author updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAuthor(Guid id)
        {
            if (!await _unitOfWork.Authors.ExistsAsync(id))
                return NotFound(ApiResponse<object>.FailureResponse("Author not found"));

            await _unitOfWork.Authors.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Author deleted successfully"));
        }

    }
}
