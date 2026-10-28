namespace Game.Models.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthSales { get; set; }

        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int PaidOrders { get; set; }

        public int TotalProducts { get; set; }
        public int LowStock { get; set; }
        public int OutOfStock { get; set; }

        // สินค้าขายดี
        public List<TopSellingProductViewModel> TopSellingProducts
            { get; set; } = new();
    }

    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}