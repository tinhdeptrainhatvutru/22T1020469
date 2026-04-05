using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.DataLayers.SQLServer;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.HR;
using System.Threading.Tasks;

namespace SV22T1020469.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung nhân viên mới
        /// </summary>
        public static async Task<int> AddEmployeeAsync(Employee employee)
        {
            return await employeeDB.AddAsync(employee);
        }

        /// <summary>
        /// Bổ sung nhân viên mới kèm theo mật khẩu (dùng cho đăng ký Admin)
        /// </summary>
        public static async Task<int> AddEmployeeWithPasswordAsync(Employee employee, string password)
        {
            return await employeeDB.AddWithPasswordAsync(employee, password);
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        public static async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            return await employeeDB.UpdateAsync(employee);
        }

        /// <summary>
        /// Xóa một nhân viên theo mã
        /// </summary>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (await employeeDB.IsUsed(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            return await employeeDB.IsUsed(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email có bị trùng lặp với nhân viên khác không
        /// </summary>
        public static async Task<bool> ValidateEmailAsync(string email, int employeeID = 0)
        {
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        /// <summary>
        /// Kiểm tra email đã được sử dụng bởi nhân viên khác hay chưa (true = đang dùng)
        /// </summary>
        public static async Task<bool> InUseEmployeeEmailAsync(string email, int excludeEmployeeID = 0)
        {
            return await employeeDB.InUseEmailAsync(email, excludeEmployeeID);
        }

        /// <summary>
        /// Kiểm tra số điện thoại đã được sử dụng bởi nhân viên khác hay chưa (true = đang dùng)
        /// </summary>
        public static async Task<bool> InUseEmployeePhoneAsync(string phone, int excludeEmployeeID = 0)
        {
            return await employeeDB.InUsePhoneAsync(phone, excludeEmployeeID);
        }

        /// <summary>
        /// Cập nhật quyền cho nhân viên
        /// </summary>
        public static async Task<bool> UpdateEmployeeRolesAsync(int employeeID, string roleNames)
        {
            return await employeeDB.UpdateRolesAsync(employeeID, roleNames);
        }

        #endregion
    }
}