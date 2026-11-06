using System.ComponentModel.DataAnnotations;

namespace Game.Models
{
    public class AdminDashboardViewModel
    {
        public int Total { get; set; }
        public Dictionary<string, int> Counts { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
        public int CountOf(string role) => Counts.TryGetValue(role, out var n) ? n : 0;
    }

    public class UserListViewModel
    {
        public List<User> Users { get; set; } = new();
        public Dictionary<string, int> Counts { get; set; } = new();
        public string? Role { get; set; }
        public string? Q { get; set; }
        public int Page { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public int CountOf(string role) => Counts.TryGetValue(role, out var n) ? n : 0;
    }

    public class UserFormViewModel
    {
        public string? Id { get; set; }   // แก้จาก int เป็น string?

        [Required(ErrorMessage = "กรุณากรอกอีเมล"), EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        public string Email { get; set; } = "";

        

        [Required(ErrorMessage = "กรุณาเลือก Role")]
        public string Role { get; set; } = "User";

        [MinLength(8, ErrorMessage = "รหัสผ่านอย่างน้อย 8 ตัวอักษร")]
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
        public string? ConfirmPassword { get; set; }
    }
}