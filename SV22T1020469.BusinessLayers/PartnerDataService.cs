using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.DataLayers.SQLServer;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020469.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến các đối tác của hệ thống:
    /// nhà cung cấp (Supplier), khách hàng (Customer) và người giao hàng (Shipper)
    /// </summary>
    public static class PartnerDataService
    {
        private static readonly ISupplierRepository supplierDB;
        private static readonly ICustomerRepository customerDB;
        private static readonly IShipperRepository shipperDB;

        static PartnerDataService()
        {
            supplierDB = new SupplierRepository(Configuration.ConnectionString);
            customerDB = new CustomerRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
        }

        #region Supplier
        public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
            => await supplierDB.ListAsync(input);

        public static async Task<Supplier?> GetSupplierAsync(int supplierID)
            => await supplierDB.GetAsync(supplierID);

        public static async Task<int> AddSupplierAsync(Supplier data)
            => await supplierDB.AddAsync(data);

        public static async Task<bool> UpdateSupplierAsync(Supplier data)
            => await supplierDB.UpdateAsync(data);

        public static async Task<bool> DeleteSupplierAsync(int supplierID)
        {
            if (await supplierDB.IsUsed(supplierID)) return false;
            return await supplierDB.DeleteAsync(supplierID);
        }

        public static async Task<bool> IsUsedSupplierAsync(int supplierID)
            => await supplierDB.IsUsed(supplierID);

        public static async Task<bool> InUseSupplierEmailAsync(string email, int excludeSupplierID = 0)
            => await supplierDB.InUseEmailAsync(email, excludeSupplierID);

        public static async Task<bool> InUseSupplierPhoneAsync(string phone, int excludeSupplierID = 0)
            => await supplierDB.InUsePhoneAsync(phone, excludeSupplierID);
        #endregion

        #region Customer
        public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
            => await customerDB.ListAsync(input);

        public static async Task<Customer?> GetCustomerAsync(int customerID)
            => await customerDB.GetAsync(customerID);

        public static async Task<int> AddCustomerAsync(Customer data)
            => await customerDB.AddAsync(data);

        /// <summary>
        /// Thêm khách hàng mới kèm theo mật khẩu (dùng cho chức năng đăng ký Shop)
        /// </summary>
        public static async Task<int> AddCustomerWithPasswordAsync(Customer data, string password)
            => await customerDB.AddWithPasswordAsync(data, password);

        public static async Task<bool> UpdateCustomerAsync(Customer data)
            => await customerDB.UpdateAsync(data);

        public static async Task<bool> DeleteCustomerAsync(int customerID)
        {
            if (await customerDB.IsUsed(customerID)) return false;
            return await customerDB.DeleteAsync(customerID);
        }

        public static async Task<bool> IsUsedCustomerAsync(int customerID)
            => await customerDB.IsUsed(customerID);

        /// <summary>
        /// Kiểm tra email có thể dùng để đăng ký không (true = hợp lệ, chưa bị trùng)
        /// </summary>
        public static async Task<bool> ValidateCustomerEmailAsync(string email, int customerID = 0)
            => await customerDB.ValidateEmailAsync(email, customerID);

        public static async Task<bool> InUseCustomerEmailAsync(string email, int excludeCustomerID = 0)
            => await customerDB.InUseEmailAsync(email, excludeCustomerID);

        public static async Task<bool> InUseCustomerPhoneAsync(string phone, int excludeCustomerID = 0)
            => await customerDB.InUsePhoneAsync(phone, excludeCustomerID);

        public static async Task<int> CountCustomersAsync()
            => await customerDB.CountAsync();
        #endregion

        #region Shipper
        public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
            => await shipperDB.ListAsync(input);

        public static async Task<Shipper?> GetShipperAsync(int shipperID)
            => await shipperDB.GetAsync(shipperID);

        public static async Task<int> AddShipperAsync(Shipper data)
            => await shipperDB.AddAsync(data);

        public static async Task<bool> UpdateShipperAsync(Shipper data)
            => await shipperDB.UpdateAsync(data);

        public static async Task<bool> DeleteShipperAsync(int shipperID)
        {
            if (await shipperDB.IsUsed(shipperID)) return false;
            return await shipperDB.DeleteAsync(shipperID);
        }

        public static async Task<bool> IsUsedShipperAsync(int shipperID)
            => await shipperDB.IsUsed(shipperID);

        public static async Task<bool> InUseShipperPhoneAsync(string phone, int excludeShipperID = 0)
            => await shipperDB.InUsePhoneAsync(phone, excludeShipperID);
        #endregion
    }
}