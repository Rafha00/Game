namespace Game.Models
{
    public class CartLine
    {
        public Guid CartItemId { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public decimal LineTotal => Price * Quantity;
    }

    public class CartViewModel
    {
        public List<CartLine> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public string? CouponCode { get; set; }
        public decimal Discount { get; set; }
        public decimal Total => Math.Max(0, Subtotal - Discount);
        public List<DiscountCode> AvailableCodes { get; set; } = new();
    }
}