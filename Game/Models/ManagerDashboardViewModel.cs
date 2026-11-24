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
        public int CancelledOrders { get; set; }

        public int TotalProducts { get; set; }
        public int LowStock { get; set; }
        public int OutOfStock { get; set; }

        // ยอดขายรายเดือน (12 เดือนล่าสุด)
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthlySales { get; set; } = new();
        public List<int> MonthlyOrders { get; set; } = new();

        // สต็อกสินค้า (เหลือน้อยที่สุด)
        public List<string> StockLabels { get; set; } = new();
        public List<int> StockValues { get; set; } = new();

        // สินค้าขายดี
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
    }

    public class TopSellingProductViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}