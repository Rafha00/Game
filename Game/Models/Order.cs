using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("discount_amount")]
        public decimal DiscountAmount { get; set; }

        [Column("coupon_code")]
        public string? CouponCode { get; set; }

        [Column("total_price")]
        public decimal TotalPrice { get; set; }

        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
    }

    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("product_id")]
        public Guid ProductId { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}