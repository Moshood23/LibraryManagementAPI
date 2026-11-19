using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models;

public class Book : BaseEntity
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "ISBN is required")]
    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Range(1000, 2100, ErrorMessage = "Publication year must be between 1000 and 2100")]
    public int PublicationYear { get; set; }

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public Guid AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public Guid GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
