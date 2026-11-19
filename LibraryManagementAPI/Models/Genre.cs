using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models;

public class Genre : BaseEntity
{
    [Required(ErrorMessage = "Genre name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
