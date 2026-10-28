using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password")] // เพิ่มส่วนรองรับรหัสผ่านให้ตรงกับฐานข้อมูล
        public string Password { get; set; } = string.Empty;

        [Column("role")]
        public string Role { get; set; } = "customer";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}