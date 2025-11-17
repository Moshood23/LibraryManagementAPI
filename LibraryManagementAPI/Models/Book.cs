using System.ComponentModel.DataAnnotations;
using LibraryManagementAPI.Model;
using Microsoft.Data.SqlClient;

namespace LibraryManagementAPI.Model
{
    public class Book : BaseEntity
    {

        [Required(ErrorMessage = "Title is required")]
        [StringLength(15)] 
        public string Title { get; set; } = string .Empty;

        [Required(ErrorMessage = "ISBN is  required")]
        [StringLength(15)]
        public string ISBN { get; set; } = string.Empty;

        [Range(500, 700, ErrorMessage = "Publication year must be between 500 and 700")]
        public int PublicationYear { get; set; }

        public Guid AuthorId { get; set; }
        public Guid GenreId { get; set; }

        public Author Author { get; set; } = null!;
        public Genre Genre { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
