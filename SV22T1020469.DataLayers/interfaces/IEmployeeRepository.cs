using SV22T1020469.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020469.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Thêm nhân viên mới kèm mật khẩu (MD5) — dùng cho đăng ký Admin
        /// </summary>
        Task<int> AddWithPasswordAsync(Employee data, string password);

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);

        /// <summary>
        /// Kiểm tra email đã được sử dụng bởi nhân viên khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUseEmailAsync(string email, int excludeEmployeeID = 0);

        /// <summary>
        /// Kiểm tra số điện thoại đã được sử dụng bởi nhân viên khác hay chưa (true = đang dùng)
        /// </summary>
        Task<bool> InUsePhoneAsync(string phone, int excludeEmployeeID = 0);

        /// <summary>
        /// Cập nhật danh sách quyền cho nhân viên
        /// </summary>
        Task<bool> UpdateRolesAsync(int employeeID, string roleNames);
    }
}
