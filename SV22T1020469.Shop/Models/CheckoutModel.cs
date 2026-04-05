using System.Collections.Generic;
using System.Linq;

namespace SV22T1020469.Shop.Models
{
    public class CheckoutModel
    {
        // Fix cảnh báo null
        public string Address { get; set; } = "";

        // Fix cảnh báo null
        public List<CartItem> Cart { get; set; } = new List<CartItem>();

        public decimal Total
        {
            get
            {
                if (Cart == null || Cart.Count == 0)
                    return 0;

                return Cart.Sum(p => p.Total);
            }
        }
    }
}