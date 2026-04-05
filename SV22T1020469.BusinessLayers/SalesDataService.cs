using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.DataLayers.SQLServer;
using SV22T1020469.Models;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020469.BusinessLayers
{
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        static SalesDataService()
        {
            orderDB =
                new OrderRepository(
                    Configuration.ConnectionString);
        }

        #region Order

        public static async Task<PagedResult<OrderViewInfo>>
            ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        public static async Task<OrderViewInfo?>
            GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        public static async Task<int>
            AddOrderAsync(Order data)
        {
            return await orderDB.AddAsync(data);
        }

        public static async Task<bool>
            UpdateOrderAsync(Order data)
        {
            return await orderDB.UpdateAsync(data);
        }

        public static async Task<bool>
            DeleteOrderAsync(int orderID)
        {
            return await orderDB.DeleteAsync(orderID);
        }

        // ⭐ FIX QUAN TRỌNG
        public static async Task<int>
            CreateOrderAsync(
            Order order,
            List<OrderDetail> details)
        {
            return await orderDB.InitOrderAsync(order, details);
        }

        public static async Task<bool> ChangeOrderStatusAsync(int orderID, OrderStatusEnum status, int? employeeID = null)
        {
            return await orderDB.UpdateOrderStatusAsync(orderID, status, employeeID);
        }

        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            return await orderDB.AcceptOrderAsync(orderID, employeeID);
        }

        public static async Task<int> CountOrdersAsync()
        {
            return await orderDB.CountAsync();
        }

        public static async Task<decimal> GetTodayRevenueAsync()
        {
            return await orderDB.GetTodayRevenueAsync();
        }

        public static async Task<List<PendingOrderItem>> ListPendingOrdersAsync()
        {
            return await orderDB.ListPendingOrdersAsync();
        }

        public static async Task<List<TopProductItem>> ListTopProductsAsync(int top = 5)
        {
            return await orderDB.ListTopProductsAsync(top);
        }

        public static async Task<List<decimal>> ListRevenueByMonthsAsync(int year)
        {
            return await orderDB.ListRevenueByMonthsAsync(year);
        }

        #endregion

        #region Order Detail

        public static async Task<List<OrderDetailViewInfo>>
            ListDetailsAsync(int orderID)
        {
            return await orderDB
                .ListDetailsAsync(orderID);
        }

        public static async Task<OrderDetailViewInfo?>
            GetDetailAsync(
                int orderID,
                int productID)
        {
            return await orderDB
                .GetDetailAsync(
                    orderID,
                    productID);
        }

        public static async Task<bool>
            AddDetailAsync(OrderDetail data)
        {
            return await orderDB
                .AddDetailAsync(data);
        }

        public static async Task<bool>
            UpdateDetailAsync(OrderDetail data)
        {
            return await orderDB
                .UpdateDetailAsync(data);
        }

        public static async Task<bool>
            DeleteDetailAsync(
                int orderID,
                int productID)
        {
            return await orderDB
                .DeleteDetailAsync(
                    orderID,
                    productID);
        }

        #endregion
    }
}