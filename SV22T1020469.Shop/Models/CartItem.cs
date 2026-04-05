namespace SV22T1020469.Shop.Models
{
    public class CartItem
    {
        public int ProductID { get; set; }
        public long AttributeID { get; set; }
        public string AttributeText { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Photo { get; set; } = "";
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Quantity * SalePrice;
    }
}
