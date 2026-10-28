using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("discounts")] // ระบุชื่อตารางในฐานข้อมูล (ปรับเปลี่ยนชื่อตามจริงถ้าจำเป็น เช่น discounts หรือ Coupon)
    public class Discount
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Column("type")]
        public string? Type { get; set; }

        [Column("value")]
        public decimal Value { get; set; }

        [Column("min_spend")]
        public decimal MinSpend { get; set; } = 0.00m;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("description")]
        public string? Description { get; set; }
   
   
    public decimal CalculateDiscount(decimal subtotal)
{
    if (subtotal < MinSpend) return 0m;

    if (Type?.ToLower() == "percent")
    {
        return subtotal * (Value / 100m);
    }
    else // Amount
    {
        return Math.Min(Value, subtotal); // ลดตามจำนวนเงินแต่ต้องไม่เกินยอดรวม
    }
}
}

    
}