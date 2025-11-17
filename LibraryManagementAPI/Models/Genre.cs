using System.Collections;
using System.ComponentModel.DataAnnotations;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Model
{
    public class Genre : BaseEntity
    {

        [Required(ErrorMessage = "Genre name is required")]
        [StringLength(100)]

        public string Name { get; set; } = string.Empty;


        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public List <Book> Books { get; set; } = new List<Book>();
        public string Title { get; internal set; }
    }
}
