
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================= REGISTER GET =================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ================= REGISTER POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email?.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "",
                    "กรุณากรอกอีเมล"
                );

                return View(model);
            }

            // ตรวจสอบอีเมลซ้ำ
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "",
                    "อีเมลนี้ถูกใช้งานแล้ว กรุณาใช้อีเมลอื่น"
                );

                return View(model);
            }

            // ตรวจสอบรหัสผ่าน
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    "",
                    "กรุณากรอกรหัสผ่าน"
                );

                return View(model);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,

                // ใช้รูปแบบเดิมของฐานข้อมูลปัจจุบัน
                // ควรเปลี่ยนเป็น Password Hash ก่อนใช้งานจริง
                Password = model.Password,

                // ผู้สมัครทั่วไปไม่สามารถกำหนด Role เองได้
                Role = "user"
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // แจ้งเตือนสมัครสมาชิกสำเร็จ
                TempData["SuccessMessage"] =
                    "สมัครสมาชิกสำเร็จ! กรุณาเข้าสู่ระบบ";

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "เกิดข้อผิดพลาดขณะสมัครสมาชิก"
                );

                ModelState.AddModelError(
                    "",
                    "ไม่สามารถสมัครสมาชิกได้ กรุณาลองใหม่อีกครั้ง"
                );

                return View(model);
            }
        }

        // ================= LOGIN GET =================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        // ================= LOGIN POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string? email,
            string? password,
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "",
                    "กรุณากรอกอีเมลและรหัสผ่าน"
                );

                return View();
            }

            try
            {
                var normalizedEmail = email.Trim();

                // ใช้การตรวจสอบแบบเดิมให้เข้ากับฐานข้อมูลปัจจุบัน
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email == normalizedEmail &&
                        u.Password == password);

                if (user == null)
                {
                    ModelState.AddModelError(
                        "",
                        "อีเมลหรือรหัสผ่านไม่ถูกต้อง"
                    );

                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.Id.ToString()
                    ),
                    new Claim(
                        ClaimTypes.Name,
                        user.Email ?? ""
                    ),
                    new Claim(
                        ClaimTypes.Email,
                        user.Email ?? ""
                    ),
                    new Claim(
                        ClaimTypes.Role,
                        user.Role ?? "user"
                    )
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var principal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal
                );

                // แจ้งเตือนเข้าสู่ระบบสำเร็จ
                TempData["SuccessMessage"] =
                    "เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับสู่ GameWorld";

                // Redirect กลับหน้าที่ต้องการ
                // อนุญาตเฉพาะ URL ภายในเว็บไซต์เท่านั้น
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "เกิดข้อผิดพลาดขณะเข้าสู่ระบบ"
                );

                ModelState.AddModelError(
                    "",
                    "เกิดข้อผิดพลาดในการเข้าสู่ระบบ กรุณาลองใหม่"
                );

                return View();
            }
        }

        // ================= LOGOUT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        // ================= PROFILE =================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!Guid.TryParse(userId, out var id))
            {
                return Unauthorized();
            }

            var userProfile = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userProfile == null)
            {
                return NotFound();
            }

            return View(userProfile);
        }
    }
}
