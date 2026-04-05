using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;

namespace SV22T1020469.DataLayers.Interfaces
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>
        /// Thêm khách hàng mới kèm mật khẩu (MD5) — dùng cho đăng ký Shop
        /// </summary>
        Task<int> AddWithPasswordAsync(Customer data, string password);

        /// <summary>
        /// Kiểm tra email có thể dùng không (true = chưa bị trùng = hợp lệ)
        /// </summary>
        Task<bool> ValidateEmailAsync(string email, int excludeCustomerID = 0);

        /// <summary>
        /// Kiểm tra email đã được sử dụng bởi khách hàng khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUseEmailAsync(string email, int excludeCustomerID = 0);

        /// <summary>
        /// Kiểm tra số điện thoại đã được sử dụng bởi khách hàng khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUsePhoneAsync(string phone, int excludeCustomerID = 0);

        Task<int> CountAsync();
    }
}
