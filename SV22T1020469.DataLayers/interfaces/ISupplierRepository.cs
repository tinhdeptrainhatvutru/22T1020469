using SV22T1020469.Models.Partner;

namespace SV22T1020469.DataLayers.Interfaces
{
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Kiểm tra email đã được sử dụng bởi nhà cung cấp khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUseEmailAsync(string email, int excludeSupplierID = 0);

        /// <summary>
        /// Kiểm tra số điện thoại đã được sử dụng bởi nhà cung cấp khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUsePhoneAsync(string phone, int excludeSupplierID = 0);
    }
}

