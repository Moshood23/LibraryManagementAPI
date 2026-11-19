using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.DTOs;

public class GenreCreateUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}

public class GenreReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int BookCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GenreWithBooksDto : GenreReadDto
{
    public List<BookReadDto> Books { get; set; } = new();
}
