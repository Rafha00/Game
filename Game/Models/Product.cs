using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("products")]
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
        public long Price { get; set; }

        [Column("image_url")]
            public string? ImageUrl { get; set; } = string.Empty;

            // สำหรับรับไฟล์จากฟอร์ม (ไม่ต้อง map ลงฐานข้อมูล)
            [NotMapped]
            public IFormFile? ImageFile { get; set; }

        [Column("category")]
        public string Category { get; set; } = "";

        [Column("stock")]
        public int Stock { get; set; }
    }
}