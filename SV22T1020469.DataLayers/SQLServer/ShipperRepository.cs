using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng (Shipper) trên SQL Server
    /// </summary>
    public class ShipperRepository : IShipperRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm mới một người giao hàng vào CSDL
        /// </summary>
        /// <param name="data">Thông tin người giao hàng cần thêm</param>
        /// <returns>Mã người giao hàng vừa được tạo (ShipperID)</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Shippers (ShipperName, Phone)
                VALUES (@ShipperName, @Phone);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.ShipperName,
                data.Phone
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        /// <summary>
        /// Xóa một người giao hàng theo mã (ID)
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Shippers WHERE ShipperID = @ShipperID";

            var parameters = new { ShipperID = id };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một người giao hàng theo mã (ID)
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Đối tượng Shipper, trả về null nếu không tìm thấy</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Shippers WHERE ShipperID = @ShipperID";

            var parameters = new { ShipperID = id };

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, parameters);
        }

        /// <summary>
        /// Kiểm tra xem người giao hàng có đang được sử dụng hay không 
        /// (Kiểm tra xem đã có đơn hàng nào do người này giao chưa)
        /// </summary>
        /// <param name="id">Mã người giao hàng cần kiểm tra</param>
        /// <returns>True nếu đang được sử dụng (không được xóa), ngược lại False</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Theo CSDL, bảng Orders có khóa ngoại ShipperID tham chiếu đến Shippers
            string sql = @"
                IF EXISTS (SELECT 1 FROM Orders WHERE ShipperID = @ShipperID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            var parameters = new { ShipperID = id };

            return await connection.ExecuteScalarAsync<bool>(sql, parameters);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách người giao hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Đối tượng PagedResult chứa danh sách dữ liệu và thông tin trang</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            // 1. Câu lệnh đếm tổng số dòng thỏa mãn điều kiện tìm kiếm
            string countSql = @"
                SELECT COUNT(*) 
                FROM Shippers 
                WHERE (@SearchValue = N'%%') 
                   OR (ShipperName LIKE @SearchValue) 
                   OR (Phone LIKE @SearchValue)";

            // 2. Câu lệnh lấy dữ liệu có phân trang
            string dataSql = @"
                SELECT * FROM Shippers 
                WHERE (@SearchValue = N'%%') 
                   OR (ShipperName LIKE @SearchValue)
                   OR (Phone LIKE @SearchValue)
                ORDER BY ShipperName";

            // Bổ sung phân trang nếu PageSize > 0
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

            // Thực thi đếm số lượng
            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Thực thi lấy danh sách dữ liệu
            var dataItems = await connection.QueryAsync<Shipper>(dataSql, parameters);

            // 3. Trả về kết quả
            return new PagedResult<Shipper>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một người giao hàng
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng đã chỉnh sửa</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Shippers 
                SET ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID";

            var parameters = new
            {
                data.ShipperName,
                data.Phone,
                data.ShipperID
            };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> InUsePhoneAsync(string phone, int excludeShipperID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Shippers
                WHERE Phone = @Phone
                AND ShipperID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Phone = (phone ?? "").Trim(),
                ExcludeID = excludeShipperID
            });

            return count > 0;
        }
    }
}