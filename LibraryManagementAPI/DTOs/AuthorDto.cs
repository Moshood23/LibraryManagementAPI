using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.DTOs
{
    public class AuthorCreateUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
    }

    public class AuthorReadDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int BookCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AuthorWithBooksDto
    { 
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;   
        public string LastName { get; set; } = string.Empty;    
        public string FullName { get; set; } =string.Empty;
        public string Bio { get; set; } = string.Empty ;
        public DateTime DateOfBirth { get; set; }
        public List<BookReadDto> Books { get; set; } = new();

    }
}
