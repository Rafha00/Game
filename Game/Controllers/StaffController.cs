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
                .Where(u => u.Role.ToLower() == "customer")
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
    
    }
}
