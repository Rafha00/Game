using Game.Data;
using Game.Models;
using Game.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // ==========================================================
        // ตั้งค่าเวลา: Order.CreatedAt ถูกเก็บเป็นเวลา UTC หรือเวลาท้องถิ่น?
        //   - ถ้าตอนสร้างออเดอร์ใช้ DateTime.UtcNow  -> ตั้งเป็น true
        //   - ถ้าตอนสร้างออเดอร์ใช้ DateTime.Now     -> ตั้งเป็น false
        // ถ้ายอดวันนี้ยังเป็น 0 ทั้งที่มีออเดอร์ชำระแล้วของวันนี้ ให้ลองสลับค่านี้
        // ==========================================================
        private const bool CreatedAtIsUtc = false;

        private static TimeZoneInfo GetThaiTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
        }

        // คืนช่วงเวลา [เริ่ม, สิ้นสุด) ของ "วันนี้" และ "เดือนนี้" ในหน่วยเดียวกับ CreatedAt
        private static (DateTime TodayStart, DateTime TodayEnd, DateTime MonthStart, DateTime MonthEnd) GetRanges()
        {
            if (CreatedAtIsUtc)
            {
                var tz = GetThaiTimeZone();
                var nowThai = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

                var today = nowThai.Date;
                var month = new DateTime(nowThai.Year, nowThai.Month, 1);

                DateTime ToUtc(DateTime d) => TimeZoneInfo.ConvertTimeToUtc(d, tz);

                return (ToUtc(today), ToUtc(today.AddDays(1)),
                        ToUtc(month), ToUtc(month.AddMonths(1)));
            }
            else
            {
                var today = DateTime.Today;
                var month = new DateTime(today.Year, today.Month, 1);

                return (today, today.AddDays(1), month, month.AddMonths(1));
            }
        }

        private static bool IsPaid(Order o) =>
            string.Equals(o.Status, "paid", StringComparison.OrdinalIgnoreCase);

        // คำนวณยอดขายรวม / วันนี้ / เดือนนี้ จากรายการออเดอร์
        private static (decimal Total, decimal Today, decimal Month) CalculateSales(IEnumerable<Order> orders)
        {
            var r = GetRanges();
            var paid = orders.Where(IsPaid).ToList();

            var total = paid.Sum(o => o.TotalPrice);

            var today = paid
                .Where(o => o.CreatedAt >= r.TodayStart && o.CreatedAt < r.TodayEnd)
                .Sum(o => o.TotalPrice);

            var month = paid
                .Where(o => o.CreatedAt >= r.MonthStart && o.CreatedAt < r.MonthEnd)
                .Sum(o => o.TotalPrice);

            return (total, today, month);
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

            var sales = CalculateSales(orders);

            var model = new ManagerDashboardViewModel
            {
                TotalSales = sales.Total,
                TodaySales = sales.Today,
                MonthSales = sales.Month,

                TotalOrders = orders.Count,
                PendingOrders = orders.Count(o => string.Equals(o.Status, "pending", StringComparison.OrdinalIgnoreCase)),
                PaidOrders = orders.Count(IsPaid),

                TotalProducts = products.Count,
                LowStock = products.Count(p => p.Stock <= 5 && p.Stock > 0),
                OutOfStock = products.Count(p => p.Stock <= 0)
            };

            return View(model);
        }

        // GET: /Manager/Reports
        public async Task<IActionResult> Reports()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Stock)
                .ToListAsync();

            ViewBag.Orders = orders;
            ViewBag.Products = products;

            var sales = CalculateSales(orders);
            ViewBag.TotalSales = sales.Total;
            ViewBag.TodaySales = sales.Today;
            ViewBag.MonthSales = sales.Month;

            // ดึงข้อมูลพนักงานตามสิทธิ์ Role
            var staffList = new List<User>();

            if (User.IsInRole("admin"))
            {
                // Admin: เห็นทั้ง Manager และ Staff
                staffList = await _context.Users
                    .Where(u => u.Role != null &&
                               (u.Role.ToLower() == "manager" || u.Role.ToLower() == "staff"))
                    .AsNoTracking()
                    .ToListAsync();
            }
            else if (User.IsInRole("manager"))
            {
                // Manager: เห็นเฉพาะ Staff
                staffList = await _context.Users
                    .Where(u => u.Role != null && u.Role.ToLower() == "staff")
                    .AsNoTracking()
                    .ToListAsync();
            }

            ViewBag.StaffList = staffList;

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

        // GET: /Manager/Discounts
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

            var normalizedCode = code.Trim().ToUpper();

            var exists = await _context.Discounts.AnyAsync(d => d.Code == normalizedCode);
            if (exists)
            {
                TempData["ManagerError"] = "รหัสส่วนลดนี้มีอยู่ในระบบแล้ว";
                return RedirectToAction(nameof(Discounts));
            }

            var discount = new Discount
            {
                Id = Guid.NewGuid().ToString(), // รองรับ varchar(36)
                Code = normalizedCode,
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

        // GET: /Manager/Staff
        public async Task<IActionResult> Staff()
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (User.IsInRole("admin"))
            {
                // Admin เห็นทั้ง manager และ staff
                query = query.Where(u => u.Role != null &&
                                        (u.Role.ToLower() == "manager" || u.Role.ToLower() == "staff"));
            }
            else if (User.IsInRole("manager"))
            {
                // Manager เห็นเฉพาะ staff
                query = query.Where(u => u.Role != null && u.Role.ToLower() == "staff");
            }

            var users = await query.OrderBy(u => u.Email).ToListAsync();

            return View(users);
        }

        // POST: /Manager/ToggleStatus (ระงับ / เปิดใช้งานบัญชี)
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

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            string statusText = user.IsActive ? "เปิดใช้งานบัญชี" : "ระงับการใช้งานบัญชี";
            TempData["ManagerSuccess"] = $"{statusText} ({email}) เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Staff));
        }

        // POST: /Manager/DeleteStaff (ลบพนักงานออกจากระบบ)
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