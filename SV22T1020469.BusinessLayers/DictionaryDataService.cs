using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.DataLayers.SQLServer;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.DataDictionary;
using SV22T1020469.Models.Partner;

namespace SV22T1020469.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến các danh mục từ điển (Tỉnh/Thành, Người giao hàng...)
    /// </summary>
    public static class DictionaryDataService
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;
        private static readonly IShipperRepository shipperDB;

        static DictionaryDataService()
        {
            provinceDB = new ProvinceRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Lấy danh sách Tỉnh/Thành phố
        /// </summary>
        public static async Task<List<Province>> ListProvincesAsync()
        {
            try
            {
                return await provinceDB.ListAsync();
            }
            catch
            {
                return new List<Province>();
            }
        }

        /// <summary>
        /// Lấy danh sách người giao hàng (Shipper)
        /// </summary>
        public static async Task<List<Shipper>> ListShippersAsync(string searchValue = "")
        {
            try
            {
                var data = await shipperDB.ListAsync(new PaginationSearchInput
                {
                    Page = 1,
                    PageSize = 0,
                    SearchValue = searchValue
                });
                return data.DataItems;
            }
            catch
            {
                return new List<Shipper>();
            }
        }
    }
}