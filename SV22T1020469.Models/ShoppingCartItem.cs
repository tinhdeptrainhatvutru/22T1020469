namespace SV22T1020469.Models
{
    public class ShoppingCartItem
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; } = "";

        public string Photo { get; set; } = "";

        public decimal SalePrice { get; set; }

        public int Quantity { get; set; }

        public int Stock { get; set; }

        public decimal Total
        {
            get
            {
                return SalePrice * Quantity;
            }
        }
    }
}