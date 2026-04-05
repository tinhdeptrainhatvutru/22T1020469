using SV22T1020469.Models.Partner;

namespace SV22T1020469.DataLayers.Interfaces
{
    public interface IShipperRepository : IGenericRepository<Shipper>
    {
        /// <summary>
        /// Kiểm tra số điện thoại đã được sử dụng bởi người giao hàng khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUsePhoneAsync(string phone, int excludeShipperID = 0);
    }
}

