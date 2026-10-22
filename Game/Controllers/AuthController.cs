using Microsoft.AspNetCore.Mvc;
using Game.Data;
using Game.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Game.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // --- หน้า Register (GET) ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // --- ระบบ Register (POST) ---
        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                // 1. ตรวจสอบว่ามีอีเมลนี้ในระบบหรือยัง (เปลี่ยนมาใช้ _context.Users ตามตาราง users)
                var existingUser = await _context.Users.FirstOrDefaultAsync(p => p.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "อีเมลนี้ถูกใช้งานแล้ว");
                    return View(model);
                }

                // 2. สร้าง ID ใหม่ (เนื่องจากตารางใช้ uuid() เป็นค่าเริ่มต้นได้ สามารถปล่อยให้ Database สร้าง หรือใส่ Guid เพิ่มได้)
                Guid newId = Guid.NewGuid();

                // 3. บันทึกลงตาราง users ผ่าน AppDbContext
                var user = new User
                {
                    Id = newId,
                    Email = model.Email,
                    Password = model.Password, // บันทึกรหัสผ่านลงตาราง
                    Role = string.IsNullOrEmpty(model.Role) ? "user" : model.Role
                };

                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.InnerException?.Message ?? ex.Message);
                }

                // สมัครเสร็จให้เด้งไปหน้า Login
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // --- หน้า Login (GET) ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // --- ระบบ Login (POST) ---
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // ค้นหาจากอีเมลและรหัสผ่านในตาราง users
            var user = await _context.Users.FirstOrDefaultAsync(p => p.Email == email && p.Password == password);
            
            if (user != null)
            {
                // 1. สร้าง Claims ระบุตัวตนผู้ใช้งาน
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "user")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 2. สั่งสร้างและบันทึกคุกกี้ล็อกอิน
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // 3. ล็อกอินสำเร็จ เปลี่ยนเส้นทางไปหน้าแรก
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            return View();
        }

        // --- ระบบ Logout ---
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // --- หน้า Profile ---
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // ดึง Email ของคนที่กำลังล็อกอินอยู่จาก Claim
            var email = User.FindFirstValue(ClaimTypes.Email);

            // ค้นหาข้อมูลจากตาราง users
            var userProfile = await _context.Users.FirstOrDefaultAsync(p => p.Email == email);

            if (userProfile == null)
            {
                return NotFound();
            }

            return View(userProfile);
        }
    }
}