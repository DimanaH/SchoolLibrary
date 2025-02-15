using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolLibrary.Models
{
    public enum UserRole { Admin, Teacher, Student }

    public class LibraryUser : IdentityUser
    {
       //[Key]
        //public string Id { get; set; }

        [Required]
        public string FirstName { get; set; } // Име

        [Required]
        public string LastName { get; set; } // Фамилия

        public DateTime BirthDate { get; set; } // Дата на раждане

        public string? Phone { get; set; } // Телефон

        //[Required]
       // public string Email { get; set; } // Емайл

        //[Required]
        //public string PasswordHash { get; set; } // Парола (запазена като хеш)

       // public UserRole Role { get; set; } // Роля (Admin, Teacher, Student)

        public DateTime RegistrationDate { get; set; } // Дата на регистриране

        
    }
}
