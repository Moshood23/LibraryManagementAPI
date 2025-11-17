using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.DTOs
{
    public class BookCreateUpdateDto
    {
        internal Guid authorId;
        internal Guid genreId;

        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public int AuthorId { get; set; }
        public int GenreId { get; set; }
    }

    public class BookReadDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public Guid GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}   

