using System.ComponentModel.DataAnnotations;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.DTOs
{
    public class BookCreateUpdateDto
    {
        internal Guid authorId;
        internal int genreId;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string ISBN { get; set; } = string.Empty;

        [Required]
        public int PublicationYear { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        public Guid GenreId { get; set; }
    }

    public class BookReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;
    }
}

