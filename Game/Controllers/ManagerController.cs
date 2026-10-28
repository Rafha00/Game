using Game.Data;
using Game.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Game.Models.ViewModels; 

namespace Game.Controllers
{
    [Authorize(Roles = "admin,manager")]
    public class ManagerController : Controller
    {
        private readonly AppDbContext _context;

        public ManagerController(AppDbContext context)
        {
            _context = context;
        }
       

        // GET: /Manager หรือ /Manager/Index
       public async Task<IActionResult> Index()
{
    var orders = await _context.Orders
        .AsNoTracking()
        .ToListAsync();

    var products = await _context.Products
        .AsNoTracking()
        .ToListAsync();

    var today = DateTime.Today;
    var monthStart = new DateTime(today.Year, today.Month, 1);

    var model = new ManagerDashboardViewModel
{
    TotalSales = orders
        .Where(o => o.Status == "paid")
        .Sum(o => (decimal?)o.TotalPrice) ?? 0m, // เติม (decimal?) และ ?? 0m ป้องกันค่า null

    TodaySales = orders
        .Where(o => o.CreatedAt.Date == today && o.Status == "paid")
        .Sum(o => (decimal?)o.TotalPrice) ?? 0m,

    MonthSales = orders
        .Where(o => o.CreatedAt >= monthStart
                   && o.CreatedAt < monthStart.AddMonths(1)
                   && o.Status == "paid")
        .Sum(o => (decimal?)o.TotalPrice) ?? 0m,

    TotalOrders = orders.Count,
    PendingOrders = orders.Count(o => o.Status == "pending"),
    PaidOrders = orders.Count(o => o.Status == "paid"),

    TotalProducts = products.Count,
    LowStock = products.Count(p => p.Stock <= 5 && p.Stock > 0),
    OutOfStock = products.Count(p => p.Stock <= 0)
};

    return View(model);
}

        // GET: /Manager/Reports
        public async Task<IActionResult> Reports()
        {
            var orders = await _context.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
            var products = await _context.Products.AsNoTracking().OrderBy(p => p.Stock).ToListAsync();

            ViewBag.Orders = orders;
            ViewBag.Products = products;

            ViewBag.TotalSales = orders
                .Where(o => string.Equals(o.Status, "paid", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalPrice);

            ViewBag.TodaySales = orders
                .Where(o => o.CreatedAt.Date == DateTime.Today && string.Equals(o.Status, "paid", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalPrice);

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ViewBag.MonthSales = orders
                .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthStart.AddMonths(1) && string.Equals(o.Status, "paid", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalPrice);

            return View();
        }

      

        // POST: /Manager/UpdateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateRole(string email, string role)
        {
            var allowedRoles = new[] { "admin", "manager", "staff", "customer" };

            if (string.IsNullOrWhiteSpace(email) || !allowedRoles.Contains(role?.ToLowerInvariant()))
            {
                TempData["ManagerError"] = "ข้อมูลอีเมลหรือบทบาทไม่ถูกต้อง";
                return RedirectToAction(nameof(Staff));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["ManagerError"] = "ไม่พบผู้ใช้งานที่ต้องการแก้ไข";
                return RedirectToAction(nameof(Staff));
            }

            user.Role = role!.ToLowerInvariant();
            await _context.SaveChangesAsync();

            TempData["ManagerSuccess"] = "แก้ไขบทบาทผู้ใช้งานเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Staff));
        }

        // GET: /Manager/Discounts// GET: /Manager/Discounts
        public async Task<IActionResult> Discounts()
        {
            var discounts = await _context.Discounts
                .AsNoTracking()
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(discounts);
        }


      // POST: /Manager/CreateDiscount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> CreateDiscount(string code, string type, decimal value, decimal minSpend, DateTime? expiresAt, string description)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["ManagerError"] = "กรุณากรอกรหัสโค้ดส่วนลด";
                return RedirectToAction(nameof(Discounts));
            }

            var exists = await _context.Discounts.AnyAsync(d => d.Code == code.Trim().ToUpper());
            if (exists)
            {
                TempData["ManagerError"] = "รหัสส่วนลดนี้มีอยู่ในระบบแล้ว";
                return RedirectToAction(nameof(Discounts));
            }

            var discount = new Discount
            {
                Id = Guid.NewGuid().ToString(), // รองรับ varchar(36)
                Code = code.Trim().ToUpper(),
                Type = type,
                Value = value,
                MinSpend = minSpend,
                IsActive = true,
                ExpiresAt = expiresAt,
                Description = description
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            TempData["ManagerSuccess"] = $"สร้างโค้ดส่วนลด ({discount.Code}) สำเร็จแล้ว";
            return RedirectToAction(nameof(Discounts));
        }

        // POST: /Manager/DeleteDiscount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> DeleteDiscount(string id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                TempData["ManagerError"] = "ไม่พบโค้ดส่วนลดที่ต้องการลบ";
                return RedirectToAction(nameof(Discounts));
            }

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            TempData["ManagerSuccess"] = "ลบโค้ดส่วนลดเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Discounts));
        }


        // GET: /Manager/Staff (ปรับปรุงให้ดึงเฉพาะพนักงานที่เป็น "staff")
        public async Task<IActionResult> Staff()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role != null && u.Role.ToLower() == "staff") // กรองเฉพาะ role เป็น staff
                .OrderBy(u => u.Email)
                .ToListAsync();

            return View(users);
        }

        // POST: /Manager/ToggleStatus (สำหรับระงับ หรือ เปิดใช้งานพนักงาน)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> ToggleStatus(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ManagerError"] = "ไม่พบข้อมูลอีเมลผู้ใช้งาน";
                return RedirectToAction(nameof(Staff));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["ManagerError"] = "ไม่พบผู้ใช้งานในระบบ";
                return RedirectToAction(nameof(Staff));
            }

            // สลับสถานะ IsActive (ถ้าเป็น true จะเปลี่ยนเป็น false, ถ้า false จะเป็น true)
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            string statusText = user.IsActive ? "เปิดใช้งานบัญชี" : "ระงับการใช้งานบัญชี";
            TempData["ManagerSuccess"] = $"{statusText} ({email}) เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Staff));
        }

        // POST: /Manager/DeleteStaff (สำหรับลบพนักงานออกจากระบบ)
        [HttpPost]
        [ValidateAntiForgeryToken]
       [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> DeleteStaff(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ManagerError"] = "ไม่พบข้อมูลอีเมลผู้ใช้งาน";
                return RedirectToAction(nameof(Staff));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["ManagerError"] = "ไม่พบผู้ใช้งานที่ต้องการลบ";
                return RedirectToAction(nameof(Staff));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["ManagerSuccess"] = $"ลบพนักงาน ({email}) เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Staff));
        }
    }
}