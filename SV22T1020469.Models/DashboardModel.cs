using SV22T1020469.Models.Sales;

namespace SV22T1020469.Models
{
    public class DashboardModel
    {
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public List<PendingOrderItem> PendingOrders { get; set; } = new();
        public List<TopProductItem> TopProducts { get; set; } = new();
        public List<decimal> RevenueByMonths { get; set; } = new();
    }

    public class PendingOrderItem
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderTime { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatusEnum Status { get; set; }
    }

    public class TopProductItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SoldQuantity { get; set; }
    }
}
