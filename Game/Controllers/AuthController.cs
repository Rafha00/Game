using Microsoft.AspNetCore.Mvc;
using Game.Data;
using Game.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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
                // 1. ตรวจสอบว่ามีอีเมลนี้ในระบบหรือยัง
                var existingUser = await _context.Profiles.FirstOrDefaultAsync(p => p.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "อีเมลนี้ถูกใช้งานแล้ว");
                    return View(model);
                }

                // 2. สร้าง ID ใหม่
                Guid newId = Guid.NewGuid();

                // 3. บันทึกลงตาราง profiles ผ่าน AppDbContext
                var profile = new ProfileModel
                {
                    Id = newId,
                    Email = model.Email,
                    Role = string.IsNullOrEmpty(model.Role) ? "customer" : model.Role
                };

                try
                {
                    _context.Profiles.Add(profile);
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
            // ค้นหาจากอีเมลในฐานข้อมูล
            var user = await _context.Profiles.FirstOrDefaultAsync(p => p.Email == email);
            
            if (user != null)
            {
                // 1. สร้าง Claims ระบุตัวตนผู้ใช้งาน
                var claims = new List<Claim>
                {
                     new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "customer")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 2. สั่งสร้างและบันทึกคุกกี้ล็อกอิน (ทำให้ User.Identity.IsAuthenticated เป็น true)
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // 3. ล็อกอินสำเร็จ เปลี่ยนเส้นทางไปหน้าแรก
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            return View();
        }

        [HttpPost]
            public async Task<IActionResult> Logout()
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();   // ล้างโค้ดส่วนลดที่ค้างใน session ด้วย
                return RedirectToAction("Index", "Home");
            }
    }
}