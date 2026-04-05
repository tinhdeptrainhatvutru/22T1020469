using System.Collections.Generic;
using System.Linq;

namespace SV22T1020469.Shop.Models
{
    public class CartModel
    {
        // Fix cảnh báo null
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal Total
        {
            get
            {
                if (Items == null || Items.Count == 0)
                    return 0;

                return Items.Sum(p => p.Total);
            }
        }
    }
}