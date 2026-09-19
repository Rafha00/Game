using Microsoft.AspNetCore.Mvc;
using Game.Models;
using Game.Data;

namespace GameWorld.Controllers
{
    public class ProductController : Controller
    {
        // 1. เปลี่ยนชื่อจาก ApplicationDbContext เป็น AppDbContext
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // หน้าเกมทั้งหมด
        public IActionResult Index()
        {
            var products = _context.Products
                .ToList();

            return View( products);
        }

        // หน้าหมวดหมู่ย่อย
            // หน้าหมวดหมู่ย่อย
public IActionResult Category(string id) 
{
    if (string.IsNullOrEmpty(id))
    {
        return RedirectToAction("Index");
    }

    var products = _context.Products
        .Where(p => p.Category != null && p.Category.ToLower() == id.ToLower())
        .ToList();

    ViewBag.CategoryName = id.ToUpper();
    return View(products); // จะวิ่งไปหาไฟล์ Category.cshtml โดยอัตโนมัติ
}

        // หน้ารายละเอียดเกม
        public IActionResult Detail(Guid id)
        {
            var product = _context.Products
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}