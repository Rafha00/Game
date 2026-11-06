using Game.Data; // หรือ namespace ที่เก็บ DbContext ของคุณ
using Game.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Game.Controllers
{
    [Authorize(Roles = "staff,manager,admin")]
    public class StaffController : Controller
    {
       private readonly AppDbContext _context;

       public StaffController(AppDbContext context)
        {
            _context = context;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var orders = _context.Orders.AsNoTracking();
            var products = _context.Products.AsNoTracking();

            ViewBag.TotalOrders = await orders.CountAsync();
            ViewBag.PendingOrders = await orders
                .CountAsync(o => o.Status == "pending");
            ViewBag.PaidOrders = await orders
                .CountAsync(o => o.Status == "paid");

            ViewBag.TotalSales = await orders
                .Where(o => o.Status == "paid")
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            ViewBag.TotalProducts = await products.CountAsync();
            ViewBag.LowStock = await products
                .CountAsync(p => p.Stock <= 5);

            ViewBag.RecentOrders = await orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.LowStockProducts = await products
                .Where(p => p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // Order management
        public async Task<IActionResult> Orders(string? status)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            ViewBag.SelectedStatus = status;

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // Product / Stock management
        public async Task<IActionResult> Products(string? search)
        {
            var query = _context.Products
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            ViewBag.Search = search;

            var products = await query
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }

        // Customer management
        public async Task<IActionResult> Customers(string? search)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Role.ToLower() == "user")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Email.Contains(search));
            }

            ViewBag.Search = search;

            var customers = await query
                .OrderBy(u => u.Email)
                .ToListAsync();

            return View(customers);
        }
    // GET: Staff/CreateProduct
        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View();
        }

       [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateProduct(Product product)
{
    // ถ้ามีการอัปโหลดไฟล์รูปภาพมา
    if (product.ImageFile != null && product.ImageFile.Length > 0)
    {
        // สร้างชื่อไฟล์ที่ไม่ซ้ำกัน (เช่น ใช้ GUID นำหน้าชื่อเดิม)
        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
        
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(product.ImageFile.FileName);
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // บันทึกไฟล์ลง Server
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await product.ImageFile.CopyToAsync(fileStream);
        }

        // เก็บ Path สำหรับเรียกใช้งานในเว็บลงฐานข้อมูล
        product.ImageUrl = "/images/products/" + uniqueFileName;
    }

    if (ModelState.IsValid)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Products));
    }
    return View(product);
}
            // POST: Staff/ToggleProduct (เปิด / ปิดการใช้งานสินค้า)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> ToggleProduct(Guid id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null)
            {
                TempData["Error"] = "ไม่พบสินค้า";
                return RedirectToAction(nameof(Products));
            }

            p.IsActive = !p.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = p.IsActive
                ? $"เปิดใช้งาน \"{p.Name}\" แล้ว"
                : $"ปิดใช้งาน \"{p.Name}\" แล้ว";
            return RedirectToAction(nameof(Products));
        }

        // POST: Staff/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> EditProduct(Guid id, string name, long price, int stock)
        {
            if (string.IsNullOrWhiteSpace(name) || price < 0 || stock < 0)
            {
                TempData["Error"] = "ข้อมูลไม่ถูกต้อง";
                return RedirectToAction(nameof(Products));
            }

            var p = await _context.Products.FindAsync(id);
            if (p == null)
            {
                TempData["Error"] = "ไม่พบสินค้า";
                return RedirectToAction(nameof(Products));
            }

            p.Name = name.Trim();
            p.Price = price;
            p.Stock = stock;
            await _context.SaveChangesAsync();

            TempData["Success"] = "บันทึกการแก้ไขสินค้าแล้ว";
            return RedirectToAction(nameof(Products));
        }

        // POST: Staff/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null)
            {
                TempData["Error"] = "ไม่พบสินค้า";
                return RedirectToAction(nameof(Products));
            }

            try
            {
                _context.Products.Remove(p);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ลบ \"{p.Name}\" แล้ว";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "ลบไม่ได้ เพราะสินค้านี้ถูกใช้งานอยู่ ลองใช้ \"ปิดการใช้งาน\" แทน";
            }
            return RedirectToAction(nameof(Products));
        }
    }
}
