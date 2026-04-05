using SV22T1020469.Models;

namespace SV22T1020469.BusinessLayers
{
    public static class ReportDataService
    {
        public static async Task<DashboardModel> GetDashboardAsync(int year)
        {
            try
            {
                var model = new DashboardModel
                {
                    TotalProducts = await CatalogDataService.CountProductsAsync(),
                    TotalCustomers = await PartnerDataService.CountCustomersAsync(),
                    TotalOrders = await SalesDataService.CountOrdersAsync(),
                    TodayRevenue = await SalesDataService.GetTodayRevenueAsync(),
                    PendingOrders = await SalesDataService.ListPendingOrdersAsync(),
                    TopProducts = await SalesDataService.ListTopProductsAsync(5),
                    RevenueByMonths = await SalesDataService.ListRevenueByMonthsAsync(year)
                };

                return model;
            }
            catch
            {
                return new DashboardModel();
            }
        }
    }
}
