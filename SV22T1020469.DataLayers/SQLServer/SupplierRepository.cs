using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp (Supplier) trên SQL Server
    /// </summary>
    public class SupplierRepository : ISupplierRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm mới một nhà cung cấp vào CSDL
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp cần thêm</param>
        /// <returns>Mã nhà cung cấp vừa được tạo (SupplierID)</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Suppliers (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.SupplierName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email
            };

            // ExecuteScalarAsync dùng để lấy giá trị được trả về từ câu lệnh SELECT SCOPE_IDENTITY()
            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        /// <summary>
        /// Xóa một nhà cung cấp theo mã (ID)
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";

            var parameters = new { SupplierID = id };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhà cung cấp theo mã (ID)
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Đối tượng Supplier, trả về null nếu không tìm thấy</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";

            var parameters = new { SupplierID = id };

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, parameters);
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được sử dụng ở bảng khác không (VD: Bảng Products)
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần kiểm tra</param>
        /// <returns>True nếu đang được sử dụng (không được xóa), ngược lại False</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Theo CSDL, bảng Products có khóa ngoại SupplierID tham chiếu đến Suppliers
            string sql = @"
                IF EXISTS (SELECT 1 FROM Products WHERE SupplierID = @SupplierID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            var parameters = new { SupplierID = id };

            return await connection.ExecuteScalarAsync<bool>(sql, parameters);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhà cung cấp dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Đối tượng PagedResult chứa danh sách dữ liệu và thông tin trang</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            // 1. Câu lệnh đếm tổng số dòng thỏa mãn điều kiện tìm kiếm
            string countSql = @"
                SELECT COUNT(*) 
                FROM Suppliers 
                WHERE (@SearchValue = N'%%') 
                   OR (SupplierName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue)";

            // 2. Câu lệnh lấy dữ liệu có phân trang
            // Nếu PageSize = 0 (hiển thị tất cả), ta không dùng OFFSET/FETCH
            string dataSql = @"
                SELECT * FROM Suppliers 
                WHERE (@SearchValue = N'%%') 
                   OR (SupplierName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue)
                ORDER BY SupplierName";

            if (input.PageSize > 0)
            {
                dataSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            var parameters = new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            // Thực thi đếm số dòng
            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Thực thi lấy dữ liệu
            var dataItems = await connection.QueryAsync<Supplier>(dataSql, parameters);

            // 3. Trả về kết quả
            return new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp đã chỉnh sửa</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Suppliers 
                SET SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            var parameters = new
            {
                data.SupplierName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                data.SupplierID
            };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> InUseEmailAsync(string email, int excludeSupplierID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Suppliers
                WHERE LOWER(Email) = @Email
                AND SupplierID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = (email ?? "").Trim().ToLowerInvariant(),
                ExcludeID = excludeSupplierID
            });

            return count > 0;
        }

        public async Task<bool> InUsePhoneAsync(string phone, int excludeSupplierID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Suppliers
                WHERE Phone = @Phone
                AND SupplierID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Phone = (phone ?? "").Trim(),
                ExcludeID = excludeSupplierID
            });

            return count > 0;
        }
    }
}