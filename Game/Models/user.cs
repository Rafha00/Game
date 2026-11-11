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
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [RegularExpression(@"^[a-zA-Z0-9_@.-]+$", ErrorMessage = "ไม่อนุญาตให้ใช้อักขระพิเศษในอีเมล")]
        public string Email { get; set; } = string.Empty;

        [Column("password")]
        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [MinLength(8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "รหัสผ่านต้องมีตัวอักษรพิมพ์เล็ก , พิมพ์ใหญ่ , ตัวเลข , และอักขระพิเศษอย่างน้อยหนึ่งตัว")]
        public string Password { get; set; } = string.Empty;

        [Column("role")]
        public string Role { get; set; } = "customer";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}