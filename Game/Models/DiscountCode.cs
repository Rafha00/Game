using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("DiscountCodes")]
    public class DiscountCode
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Code { get; set; } = "";          // เก็บเป็นตัวพิมพ์ใหญ่เสมอ เช่น WELCOME10
        public string Type { get; set; } = "percent";   // "percent" = ลดเป็น % , "fixed" = ลดเป็นบาท
        public decimal Value { get; set; }
        public decimal MinSpend { get; set; }           // ยอดซื้อขั้นต่ำ (0 = ไม่มีขั้นต่ำ)
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }        // เวลาแบบ UTC, null = ไม่หมดอายุ
        public string? Description { get; set; }

        public decimal CalculateDiscount(decimal subtotal)
        {
            var d = Type == "percent"
                ? Math.Round(subtotal * Value / 100m, 2)
                : Value;
            return Math.Min(d, subtotal);   // ส่วนลดต้องไม่เกินราคาเต็ม
        }
    }
}