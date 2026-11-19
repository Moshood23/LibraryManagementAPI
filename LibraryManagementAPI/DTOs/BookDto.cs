using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.DTOs;

public class BookCreateUpdateDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Range(1000, 2100, ErrorMessage = "Publication year must be between 1000 and 2100")]
    public int PublicationYear { get; set; }

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid AuthorId { get; set; }

    [Required]
    public Guid GenreId { get; set; }
}

public class BookReadDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public Guid GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
