using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolLibrary.Models
{
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("LibraryUser")]
        public string LibraryUserId { get; set; }
        public LibraryUser LibraryUser { get; set; } // Връзка към потребителя

        public DateTime LoginTime { get; set; } // Време на логване

        public DateTime? LogoutTime { get; set; } // Време на излизане (ако е null, потребителят все още е логнат)

        public string IPAddress { get; set; } // IP адрес на потребителя (по избор)
    }
}
