using System.Linq;
using System.Threading.Tasks;
using Game.Data;
using Game.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ดึง Role ของทุกคนมานับในหน่วยความจำ
            // เพื่อให้ตัวพิมพ์ใหญ่เล็ก/ค่า null ไม่ทำให้นับพลาด
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

            var vm = new AdminDashboardViewModel
            {
                Total = roles.Count,
                Counts = counts,
                RecentUsers = await _db.Users
                    .AsNoTracking()
                    .OrderByDescending(u => u.Id)
                    .Take(6)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}