using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Game.Data;
using Game.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.Controllers
{
    // เฉพาะ Admin เท่านั้นที่จัดการบัญชีผู้ใช้ได้
    [Authorize(Roles = "admin")]
    public class AdminUsersController : Controller
    {
        private static readonly string[] AllRoles =
        {
            "admin",
            "manager",
            "staff",
            "user"
        };

        private const int PageSize = 10;

        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _hasher;

        public AdminUsersController(AppDbContext db)
        {
            _db = db;
            _hasher = new PasswordHasher<User>();
        }

        // อีเมลของ Admin ที่กำลัง Login
        private string CurrentUserEmail =>
            User.FindFirstValue(ClaimTypes.Email)
            ?? User.Identity?.Name
            ?? string.Empty;

        private void SetPassword(User user, string password)
        {
            user.Password = _hasher.HashPassword(user, password);
        }

        private static bool IsValidRole(string? role)
        {
            return !string.IsNullOrWhiteSpace(role)
                && AllRoles.Contains(role);
        }

        // ==========================================
        // รายชื่อผู้ใช้
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? role,
            string? q,
            int page = 1)
        {
            var query = _db.Users.AsNoTracking().AsQueryable();

            // กรอง Role
            if (!string.IsNullOrWhiteSpace(role))
            {
                role = role.Trim().ToLowerInvariant();

                if (IsValidRole(role))
                {
                    query = query.Where(u => u.Role.ToLower() == role);
                }
                else
                {
                    role = null;
                }
            }

            // ค้นหา Email
            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                query = query.Where(u => u.Email.Contains(keyword));
            }

            int total = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(total / (double)PageSize);

            page = Math.Max(1, page);
            if (totalPages > 0)
            {
                page = Math.Min(page, totalPages);
            }

            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // นับจำนวนแยกตาม Role (ทำในหน่วยความจำ กัน null / ตัวพิมพ์ต่างกัน)
            var roles = await _db.Users
                .AsNoTracking()
                .Select(u => u.Role)
                .ToListAsync();

            var counts = roles
                .Select(r => string.IsNullOrWhiteSpace(r)
                    ? "user"
                    : r.Trim().ToLowerInvariant())
                .GroupBy(r => r)
                .ToDictionary(g => g.Key, g => g.Count());

            var vm = new UserListViewModel
            {
                Users = users,
                Counts = counts,
                Role = role,
                Q = q,
                Page = page,
                TotalPages = totalPages,
                Total = total
            };

            return View(vm);
        }

        // ==========================================
        // เพิ่มผู้ใช้ GET
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new UserFormViewModel
            {
                Role = "user"
            });
        }

        // ==========================================
        // เพิ่มผู้ใช้ POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            vm.Email = vm.Email?.Trim() ?? string.Empty;
            vm.Role = vm.Role?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(
                    nameof(vm.Password),
                    "กรุณากรอกรหัสผ่าน");
            }
            else if (vm.Password.Length < 8)
            {
                ModelState.AddModelError(
                    nameof(vm.Password),
                    "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร");
            }

            if (!IsValidRole(vm.Role))
            {
                ModelState.AddModelError(
                    nameof(vm.Role),
                    "Role ไม่ถูกต้อง");
            }

            var emailLower = vm.Email.ToLower();
            bool emailExists = await _db.Users.AnyAsync(u =>
                u.Email.ToLower() == emailLower);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(vm.Email),
                    "อีเมลนี้ถูกใช้งานแล้ว");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = vm.Email,
                Role = vm.Role,
                IsActive = true
            };

            SetPassword(user, vm.Password!);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "เพิ่มผู้ใช้เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // แก้ไขผู้ใช้ GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var vm = new UserFormViewModel
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                Role = string.IsNullOrWhiteSpace(user.Role)
                    ? "user"
                    : user.Role.ToLowerInvariant()
            };

            return View(vm);
        }

        // ==========================================
        // แก้ไขผู้ใช้ POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UserFormViewModel vm)
        {
            vm.Id = id.ToString();

            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            vm.Email = vm.Email?.Trim() ?? string.Empty;
            vm.Role = vm.Role?.Trim().ToLowerInvariant() ?? string.Empty;

            // ตรวจ Email ซ้ำ
            var emailLower = vm.Email.ToLower();
            bool emailExists = await _db.Users.AnyAsync(u =>
                u.Email.ToLower() == emailLower
                && u.Id != id);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(vm.Email),
                    "อีเมลนี้ถูกใช้งานแล้ว");
            }

            // ตรวจ Role
            if (!IsValidRole(vm.Role))
            {
                ModelState.AddModelError(
                    nameof(vm.Role),
                    "Role ไม่ถูกต้อง");
            }

            // ป้องกันลดสิทธิ์บัญชีตัวเอง
            bool isOwnAccount = string.Equals(
                user.Email,
                CurrentUserEmail,
                StringComparison.OrdinalIgnoreCase);

            if (isOwnAccount && vm.Role != "admin")
            {
                ModelState.AddModelError(
                    nameof(vm.Role),
                    "ไม่สามารถลดสิทธิ์บัญชีของตัวเองได้");
            }

            // ตรวจรหัสผ่านใหม่ (ถ้ากรอก)
            if (!string.IsNullOrWhiteSpace(vm.Password)
                && vm.Password.Length < 8)
            {
                ModelState.AddModelError(
                    nameof(vm.Password),
                    "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            user.Email = vm.Email;
            user.Role = vm.Role;

            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                SetPassword(user, vm.Password);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "บันทึกการแก้ไขเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // ลบผู้ใช้ POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // ป้องกันลบบัญชีตัวเอง
            if (string.Equals(
                user.Email,
                CurrentUserEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "ไม่สามารถลบบัญชีของตัวเองได้";
                return RedirectToAction(nameof(Index));
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "ลบผู้ใช้เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }
    }
}