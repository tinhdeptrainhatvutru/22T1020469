using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Common;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho loại hàng (Category) trên SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm mới một loại hàng vào CSDL
        /// </summary>
        /// <param name="data">Thông tin loại hàng cần thêm</param>
        /// <returns>Mã loại hàng vừa được tạo (CategoryID)</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Categories (CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.CategoryName,
                data.Description
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        /// <summary>
        /// Xóa một loại hàng theo mã (ID)
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";

            var parameters = new { CategoryID = id };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một loại hàng theo mã (ID)
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Đối tượng Category, trả về null nếu không tìm thấy</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Categories WHERE CategoryID = @CategoryID";

            var parameters = new { CategoryID = id };

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, parameters);
        }

        /// <summary>
        /// Kiểm tra xem loại hàng có đang được sử dụng hay không 
        /// (Kiểm tra xem đã có mặt hàng nào thuộc loại hàng này chưa)
        /// </summary>
        /// <param name="id">Mã loại hàng cần kiểm tra</param>
        /// <returns>True nếu đang được sử dụng (không được xóa), ngược lại False</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Bảng Products có khóa ngoại CategoryID tham chiếu đến Categories
            string sql = @"
                IF EXISTS (SELECT 1 FROM Products WHERE CategoryID = @CategoryID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            var parameters = new { CategoryID = id };

            return await connection.ExecuteScalarAsync<bool>(sql, parameters);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách loại hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Đối tượng PagedResult chứa danh sách dữ liệu và thông tin trang</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            // 1. Câu lệnh đếm tổng số dòng thỏa mãn điều kiện tìm kiếm
            string countSql = @"
                SELECT COUNT(*) 
                FROM Categories 
                WHERE (@SearchValue = N'%%') 
                   OR (CategoryName LIKE @SearchValue) 
                   OR (Description LIKE @SearchValue)";

            // 2. Câu lệnh lấy dữ liệu có phân trang
            string dataSql = @"
                SELECT * FROM Categories 
                WHERE (@SearchValue = N'%%') 
                   OR (CategoryName LIKE @SearchValue)
                   OR (Description LIKE @SearchValue)
                ORDER BY CategoryName";

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

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var dataItems = await connection.QueryAsync<Category>(dataSql, parameters);

            return new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một loại hàng
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng đã chỉnh sửa</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Categories 
                SET CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID";

            var parameters = new
            {
                data.CategoryName,
                data.Description,
                data.CategoryID
            };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
    }
}