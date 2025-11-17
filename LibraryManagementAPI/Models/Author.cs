using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LibraryManagementAPI.Model;

namespace LibraryManagementAPI.Model
{
    public class Author : BaseEntity
    {

        [Required(ErrorMessage = "First name is required")]
        [StringLength(20)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(20)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Bio { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public List<Book> Books { get; set; } = new List<Book>();

    }
}
