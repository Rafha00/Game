using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } // เปลี่ยนเป็น Guid ตาม type uuid ใน Supabase[cite: 9]

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("description")]
        public string Description { get; set; } = "";

        [Column("price")]
        public decimal Price { get; set; }

        [Column("image_url")] // ตรงกับในภาพที่เป็น image_url[cite: 9]
        public string? ImageUrl { get; set; } = "";

        [Column("category")]
        public string Category { get; set; } = "";

        [Column("stock")]
        public int Stock { get; set; }
    }
}