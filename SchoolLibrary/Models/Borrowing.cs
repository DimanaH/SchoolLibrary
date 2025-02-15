using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolLibrary.Models
{
    public class Borrowing
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Book")]
        public int BookId { get; set; }
        public Book Book { get; set; } // Връзка към книгата

        [ForeignKey("LibraryUser")]
        public string LibraryUserId { get; set; }
        public LibraryUser LibraryUser { get; set; } // Връзка към потребителя

        public DateTime BorrowDate { get; set; } // Дата на заемане

        public DateTime DueDate { get; set; } // Срок за връщане

        public DateTime? ReturnDate { get; set; } // Дата на връщане (ако е null, книгата не е върната)

        // Свойство, което показва дали книгата е върната
        public bool IsReturned => ReturnDate.HasValue;
    }
}
