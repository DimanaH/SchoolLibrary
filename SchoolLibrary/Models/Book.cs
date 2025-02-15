using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolLibrary.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; } // Уникален идентификатор на записа

        [Required]
        public string InventoryNumber { get; set; } // Инвентарен номер (уникален за всяко копие)

        public DateTime DateAdded { get; set; } // Дата на вписване

        [Required]
        public string Title { get; set; } // Заглавие

        [Required]
        public string Author { get; set; } // Автор

        public string ISBN { get; set; } // Международен стандартен номер

        public string Genre { get; set; } // Жанр (категория)

        public string Publisher { get; set; } // Издател

        public int PublicationYear { get; set; } // Година на издаване

        [DataType(DataType.Currency)]
        public decimal Price { get; set; } // Цена

        public bool IsAvailable { get; set; } = true; // Дали книгата е налична за заемане
    }
}
