using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customer) trên SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ── Thêm mới (không có mật khẩu – dùng ở Admin) ──────────────────────
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Customers
                    (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES
                    (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                CustomerName = (data.CustomerName ?? "").Trim(),
                ContactName = (data.ContactName ?? "").Trim(),
                Province = (data.Province ?? "").Trim(),
                Address = (data.Address ?? "").Trim(),
                Phone = (data.Phone ?? "").Trim(),
                Email = (data.Email ?? "").Trim().ToLower(),
                IsLocked = data.IsLocked ?? false
            });
        }

        // ── Thêm mới kèm mật khẩu (dùng ở Shop – chức năng đăng ký) ──────────
        public async Task<int> AddWithPasswordAsync(Customer data, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Customers
                    (CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                VALUES
                    (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, 0);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                CustomerName = (data.CustomerName ?? "").Trim(),
                ContactName = string.IsNullOrWhiteSpace(data.ContactName)
                    ? (data.CustomerName ?? "").Trim()
                    : (data.ContactName ?? "").Trim(),
                Province = (data.Province ?? "").Trim(),
                Address = (data.Address ?? "").Trim(),
                Phone = (data.Phone ?? "").Trim(),
                Email = (data.Email ?? "").Trim().ToLower(),
                Password = ToMD5((password ?? "").Trim())
            });
        }

        // ── Kiểm tra email trùng ───────────────────────────────────────────────
        public async Task<bool> ValidateEmailAsync(string email, int excludeCustomerID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM   Customers
                WHERE  Email = @Email
                AND    CustomerID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = email.Trim().ToLower(),
                ExcludeID = excludeCustomerID
            });
            return count == 0;   // true = chưa bị trùng = hợp lệ
        }

        public async Task<bool> InUseEmailAsync(string email, int excludeCustomerID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Customers
                WHERE LOWER(Email) = @Email
                AND CustomerID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = (email ?? "").Trim().ToLowerInvariant(),
                ExcludeID = excludeCustomerID
            });

            return count > 0;
        }

        public async Task<bool> InUsePhoneAsync(string phone, int excludeCustomerID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Customers
                WHERE Phone = @Phone
                AND CustomerID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Phone = (phone ?? "").Trim(),
                ExcludeID = excludeCustomerID
            });

            return count > 0;
        }

        public async Task<int> CountAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers");
        }

        // ── Xóa ───────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
            return await connection.ExecuteAsync(sql, new { CustomerID = id }) > 0;
        }

        // ── Lấy theo ID ───────────────────────────────────────────────────────
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Customer>(
                "SELECT * FROM Customers WHERE CustomerID = @CustomerID",
                new { CustomerID = id });
        }

        // ── Kiểm tra đang dùng ────────────────────────────────────────────────
        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM Orders WHERE CustomerID = @CustomerID
                ) THEN 1 ELSE 0 END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
        }

        // ── Danh sách phân trang ──────────────────────────────────────────────
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string sv = $"%{input.SearchValue}%";

            string condition = @"
                (@sv = N'%%'
                 OR CustomerName LIKE @sv
                 OR ContactName  LIKE @sv
                 OR Phone        LIKE @sv
                 OR Email        LIKE @sv)";

            string countSql = $"SELECT COUNT(*) FROM Customers WHERE {condition}";
            string dataSql  = $@"
                SELECT * FROM Customers
                WHERE  {condition}
                ORDER  BY CustomerName";

            if (input.PageSize > 0)
                dataSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var p = new { sv, Offset = input.Offset, PageSize = input.PageSize };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, p);
            var items    = await connection.QueryAsync<Customer>(dataSql, p);

            return new PagedResult<Customer>
            {
                Page      = input.Page,
                PageSize  = input.PageSize,
                RowCount  = rowCount,
                DataItems = items.ToList()
            };
        }

        // ── Cập nhật ─────────────────────────────────────────────────────────
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Customers
                SET    CustomerName = @CustomerName,
                       ContactName  = @ContactName,
                       Province     = @Province,
                       Address      = @Address,
                       Phone        = @Phone,
                       Email        = @Email,
                       IsLocked     = @IsLocked
                WHERE  CustomerID   = @CustomerID";

            return await connection.ExecuteAsync(sql, new
            {
                data.CustomerName,
                data.ContactName,
                data.Province,
                data.Address,
                data.Phone,
                data.Email,
                IsLocked = data.IsLocked ?? false,
                data.CustomerID
            }) > 0;
        }

        // ── Helper MD5 ────────────────────────────────────────────────────────
        private static string ToMD5(string input)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
