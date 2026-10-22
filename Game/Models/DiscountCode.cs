using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("discountscode")]
    public class DiscountCode
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("code")]
        public string Code { get; set; } = "";

        [Column("type")]
        public string Type { get; set; } = "percent";

        [Column("value")]
        public decimal Value { get; set; }

        [Column("min_spend")]
        public decimal MinSpend { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        
        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        public decimal CalculateDiscount(decimal subtotal)
        {
            var d = Type == "percent"
                ? Math.Round(subtotal * Value / 100m, 2)
                : Value;
            return Math.Min(d, subtotal);
        }
    }
}