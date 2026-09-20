using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Game.Data;
using Game.Models;

namespace Game.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _db;
        public CartController(AppDbContext db) => _db = db;

        private Guid? GetUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var uid) ? uid : null;
        }

        [HttpPost]   // ห้ามใส่ [Authorize] เพราะต้องตอบ 401 ให้ popup แทนการ redirect
        public async Task<IActionResult> Add(Guid productId, int qty = 1)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "กรุณาเข้าสู่ระบบก่อนสั่งซื้อสินค้า" });

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { success = false, message = "ไม่พบสินค้า" });
            if (product.Stock <= 0)
                return BadRequest(new { success = false, message = "สินค้าหมด" });

            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (item == null)
            {
                _db.CartItems.Add(new CartItem { UserId = userId.Value, ProductId = productId, Quantity = qty });
            }
            else
            {
                if (item.Quantity + qty > product.Stock)
                    return BadRequest(new { success = false, message = "จำนวนในตะกร้าเกินสต็อกที่มี" });
                item.Quantity += qty;
            }

            await _db.SaveChangesAsync();

            var count = await _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { success = true, message = "เพิ่มลงตะกร้าแล้ว", count });
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = GetUserId();
            if (userId == null) return Json(new { count = 0 });

            var count = await _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { count });
        }

       private const string CouponKey = "CouponCode";

public async Task<IActionResult> Index()
{
    var userId = GetUserId();
    if (userId == null)
        return RedirectToAction("Login", "Auth", new { returnUrl = "/Cart" });

    return View(await BuildCartAsync(userId.Value));
}

[HttpPost]
public async Task<IActionResult> UpdateQty(Guid id, int delta)
{
    var userId = GetUserId();
    if (userId == null) return RedirectToAction("Login", "Auth", new { returnUrl = "/Cart" });

    var item = await _db.CartItems.Include(c => c.Product)
        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    if (item != null)
    {
        var newQty = item.Quantity + delta;
        if (newQty <= 0)
            _db.CartItems.Remove(item);
        else if (item.Product != null && newQty > item.Product.Stock)
            TempData["CartError"] = $"สินค้านี้มีสต็อกเพียง {item.Product.Stock} ชิ้น";
        else
            item.Quantity = newQty;

        await _db.SaveChangesAsync();
    }
    return RedirectToAction(nameof(Index));
}

[HttpPost]
public async Task<IActionResult> Remove(Guid id)
{
    var userId = GetUserId();
    if (userId == null) return RedirectToAction("Login", "Auth", new { returnUrl = "/Cart" });

    var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    if (item != null)
    {
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
    }
    return RedirectToAction(nameof(Index));
}

[HttpPost]
public async Task<IActionResult> ApplyCoupon(string code)
{
    var userId = GetUserId();
    if (userId == null) return RedirectToAction("Login", "Auth", new { returnUrl = "/Cart" });

    if (string.IsNullOrWhiteSpace(code))
    {
        TempData["CouponError"] = "กรุณากรอกโค้ดส่วนลด";
        return RedirectToAction(nameof(Index));
    }

    var subtotal = await _db.CartItems
        .Where(c => c.UserId == userId)
        .SumAsync(c => c.Quantity * c.Product!.Price);

    var (coupon, error) = await ValidateCouponAsync(code, subtotal);
    if (coupon == null)
    {
        TempData["CouponError"] = error;
    }
    else
    {
        HttpContext.Session.SetString(CouponKey, coupon.Code);
        TempData["CouponOk"] = $"ใช้โค้ด {coupon.Code} สำเร็จ";
    }
    return RedirectToAction(nameof(Index));
}

[HttpPost]
public IActionResult RemoveCoupon()
{
    HttpContext.Session.Remove(CouponKey);
    return RedirectToAction(nameof(Index));
}

// ---------- helpers ----------

private async Task<(DiscountCode? coupon, string? error)> ValidateCouponAsync(string code, decimal subtotal)
{
    code = code.Trim().ToUpper();
    var coupon = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Code == code);

    if (coupon == null || !coupon.IsActive)
        return (null, "ไม่พบโค้ดส่วนลดนี้");
    if (coupon.ExpiresAt != null && coupon.ExpiresAt < DateTime.UtcNow)
        return (null, "โค้ดนี้หมดอายุแล้ว");
    if (subtotal < coupon.MinSpend)
        return (null, $"ต้องซื้อขั้นต่ำ ฿{coupon.MinSpend:N2} จึงจะใช้โค้ดนี้ได้");

    return (coupon, null);
}

private async Task<CartViewModel> BuildCartAsync(Guid userId)
{
    var items = await _db.CartItems
        .Where(c => c.UserId == userId)
        .Include(c => c.Product)
        .OrderBy(c => c.AddedAt)
        .ToListAsync();

    var vm = new CartViewModel
    {
        Items = items.Where(c => c.Product != null).Select(c => new CartLine
        {
            CartItemId = c.Id,
            ProductId = c.ProductId,
            Name = c.Product!.Name,
            Category = c.Product.Category,
            ImageUrl = c.Product.ImageUrl,
            Price = c.Product.Price,
            Quantity = c.Quantity,
            Stock = c.Product.Stock
        }).ToList()
    };
    vm.Subtotal = vm.Items.Sum(i => i.LineTotal);

    // คำนวณส่วนลดใหม่ทุกครั้งจากข้อมูลจริงใน DB ไม่เชื่อค่าจากฝั่งหน้าเว็บ
    var code = HttpContext.Session.GetString(CouponKey);
    if (!string.IsNullOrEmpty(code))
    {
        var (coupon, error) = await ValidateCouponAsync(code, vm.Subtotal);
        if (coupon != null)
        {
            vm.CouponCode = coupon.Code;
            vm.Discount = coupon.CalculateDiscount(vm.Subtotal);
        }
        else
        {
            HttpContext.Session.Remove(CouponKey);   // เช่น ยอดลดลงจนต่ำกว่าขั้นต่ำ
            TempData["CouponError"] = error;
        }
    }

    vm.AvailableCodes = await _db.DiscountCodes
        .Where(d => d.IsActive && (d.ExpiresAt == null || d.ExpiresAt > DateTime.UtcNow))
        .OrderBy(d => d.MinSpend)
        .ToListAsync();

    return vm;
}
    }
}