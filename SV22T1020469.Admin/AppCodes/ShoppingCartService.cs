using SV22T1020605.Admin.AppCodes;
using SV22T1020469.Models.Sales;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020605.Admin
{
    /// <summary>
    /// Cung cấp các chức năng xử lý giỏ hàng (lưu trong session)
    /// </summary>
    public static class ShoppingCartService
    {
        /// <summary>
        /// Tên key lưu trong session
        /// </summary>
        private const string CART = "ShoppingCart";

        /// <summary>
        /// Lấy toàn bộ giỏ hàng
        /// </summary>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);

            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }

            return cart;
        }

        /// <summary>
        /// Lấy 1 sản phẩm trong giỏ hàng
        /// </summary>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            return GetShoppingCart().FirstOrDefault(m => m.ProductID == productID);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ
        /// </summary>
        public static void AddCartItem(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();

            var existsItem = cart.FirstOrDefault(m => m.ProductID == item.ProductID);

            if (existsItem == null)
            {
                cart.Add(item);
            }
            else
            {
                // 🔥 CỘNG DỒN SỐ LƯỢNG (chuẩn shop)
                existsItem.Quantity += item.Quantity;

                // cập nhật giá mới (nếu cần)
                existsItem.SalePrice = item.SalePrice;
            }

            ApplicationContext.SetSessionData(CART, cart);
        }

        /// <summary>
        /// Cập nhật số lượng và giá
        /// </summary>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();

            var item = cart.FirstOrDefault(m => m.ProductID == productID);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    // 🔥 nếu số lượng <= 0 thì xóa luôn
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                    item.SalePrice = salePrice;
                }

                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa 1 sản phẩm
        /// </summary>
        public static void RemoveCartItem(int productID)
        {
            var cart = GetShoppingCart();

            var item = cart.FirstOrDefault(m => m.ProductID == productID);

            if (item != null)
            {
                cart.Remove(item);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            ApplicationContext.SetSessionData(CART, new List<OrderDetailViewInfo>());
        }

        /// <summary>
        /// Tính tổng tiền giỏ hàng
        /// </summary>
        public static decimal GetTotalAmount()
        {
            return GetShoppingCart().Sum(m => m.Quantity * m.SalePrice);
        }

        /// <summary>
        /// Tổng số lượng sản phẩm
        /// </summary>
        public static int GetTotalQuantity()
        {
            return GetShoppingCart().Sum(m => m.Quantity);
        }
    }
}