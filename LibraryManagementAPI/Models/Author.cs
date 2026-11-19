using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models;

public class Author : BaseEntity
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Bio { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
