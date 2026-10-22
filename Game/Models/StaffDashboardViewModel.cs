using Game.Data;
namespace Game.ViewModels
{
    public class StaffDashboardViewModel
    {
        // สรุปข้อมูล Dashboard
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalCustomers { get; set; }

        // ยอดขายรวม
        public decimal TotalSales { get; set; }

        // รายการคำสั่งซื้อล่าสุด
        public List<RecentOrderViewModel> RecentOrders { get; set; }
            = new List<RecentOrderViewModel>();

        // สินค้าที่ใกล้หมด
        public List<LowStockProductViewModel> LowStockItems { get; set; }
            = new List<LowStockProductViewModel>();
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }

    public class LowStockProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}
