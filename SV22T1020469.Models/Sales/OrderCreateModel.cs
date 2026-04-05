using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Partner;
using System.Collections.Generic;

namespace SV22T1020469.Models.Sales
{
    public class OrderCreateModel
    {
        public string SearchValue { get; set; } = "";

        public List<Product> Products { get; set; } = new List<Product>();

        public List<Customer> Customers { get; set; } = new List<Customer>();

        // 🔥 Đã sửa kiểu dữ liệu thành OrderDetailViewInfo để khớp với View ShowCart
        public List<OrderDetailViewInfo> Cart { get; set; } = new List<OrderDetailViewInfo>();
    }
}