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

        // หน้าเกมทั้งหมด (GET: /Product หรือ /Product/Index)
        public IActionResult Index()
        {
            var products = _context.Products
                .ToList();

            return View(products);
        }

        // หน้าหมวดหมู่ย่อย (GET: /Product/Category/{id})
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

        // หน้ารายละเอียดเกม (GET: /Product/Detail/{id})
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

        // ฟังก์ชันเพิ่มเติมสำหรับรับข้อมูลแบบ POST จากโปรแกรมทดสอบ (POST: /Product/Create)
        [HttpPost]
        public IActionResult Create([FromBody] Product product)
        {
            // ตรวจสอบว่ามีข้อมูลส่งมาหรือไม่
            if (product == null)
            {
                return BadRequest(new { message = "ข้อมูลสินค้าไม่ถูกต้อง หรือส่งมาไม่ครบถ้วน" });
            }

            // ถ้าค่า Id ของ Guid เป็นค่าว่าง ให้สร้าง Guid ใหม่ขึ้นมา
            if (product.Id == Guid.Empty)
            {
                product.Id = Guid.NewGuid();
            }

            try
            {
                // บันทึกข้อมูลลงฐานข้อมูล
                _context.Products.Add(product);
                _context.SaveChanges();

                // ส่งสถานะ 201 Created กลับไป พร้อมข้อมูลที่สร้างสำเร็จ
                return CreatedAtAction(nameof(Detail), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                // หากระบบฐานข้อมูลเกิดข้อผิดพลาด จะส่งสถานะ 500 กลับไป
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดภายในเซิร์ฟเวอร์", error = ex.Message });
            }
        }
      
      
            // ฟังก์ชันรองรับการกดปุ่มชำระเงิน (POST: /Product/Pay)
               // ฟังก์ชันรองรับการกดปุ่มชำระเงิน (POST: /Product/Pay)
        [HttpPost]
        public IActionResult Pay(Guid id, string method)
        {
            // 1. ตรวจสอบว่ามีข้อมูล ID คำสั่งซื้อ และวิธีชำระเงินส่งมาครบถ้วนไหม
            if (id == Guid.Empty || string.IsNullOrEmpty(method))
            {
                return BadRequest(new { message = "ข้อมูลการชำระเงินไม่ถูกต้อง หรือส่งมาไม่ครบถ้วน" });
            }

            try
            {
                // 2. โค้ดส่วนนี้สำหรับอัปเดตสถานะในฐานข้อมูลของคุณ (ตัวอย่างการใช้งาน)
                // var order = _context.Orders.FirstOrDefault(o => o.Id == id);
                // if (order != null) 
                // {
                //     order.Status = "Paid";       // เปลี่ยนสถานะเป็นจ่ายเงินแล้ว
                //     order.PaymentMethod = method; // บันทึกวิธีชำระเงิน (visa / promptpay)
                //     _context.SaveChanges();
                // }

                // [แก้ไขเพิ่ม] เก็บข้อความแจ้งเตือนชำระเงินสำเร็จส่งไปยังหน้าถัดไป
                TempData["PaymentSuccess"] = "ชำระเงินสำเร็จเรียบร้อยแล้ว!";

                // 3. เมื่อระบบทำงานเสร็จสิ้น ให้สั่งโหลดหน้าเดิมใหม่ (หน้า Index)
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการบันทึกข้อมูลการชำระเงิน", error = ex.Message });
            }
        }


    
    }
}
