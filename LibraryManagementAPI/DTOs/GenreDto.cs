using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.DTOs
{
    public class GenreCreateUpdateDto
    {
        public string name { get; set; } = default!;
        public string Name { get; internal set; }
        public string description { get; set; } = default!;
        public string Description { get; internal set; }
    }

    public class GenreReadDto
    {
        public int Id { get; set; }
        public string? Name { get; set; } 
        public string? Description { get; set; } 
        public int BookCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }

    public class GenreWithBooksDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;    
        public string Description { get; set; } =string.Empty;
        public List<BookReadDto> books { get; set; } = new();
    }
}
