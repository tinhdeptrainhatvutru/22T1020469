using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.HR;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho Nhân viên trên SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.FullName,
                data.BirthDate,
                data.Address,
                data.Phone,
                data.Email,
                data.Photo,
                data.IsWorking
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<int> AddWithPasswordAsync(Employee data, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames, Password)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking, @RoleNames, @Password);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                FullName = (data.FullName ?? "").Trim(),
                data.BirthDate,
                Address = (data.Address ?? "").Trim(),
                Phone = (data.Phone ?? "").Trim(),
                Email = (data.Email ?? "").Trim().ToLowerInvariant(),
                Photo = (data.Photo ?? "").Trim(),
                IsWorking = data.IsWorking ?? true,
                RoleNames = (data.RoleNames ?? "").Trim(),
                Password = ToMD5((password ?? "").Trim())
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.ExecuteAsync(sql, new { EmployeeID = id }) > 0;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<bool> IsUsed(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Nhân viên không được xóa nếu đã từng phụ trách/duyệt một đơn hàng nào đó
            string sql = @"
                IF EXISTS (SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            string condition = @"
                (@SearchValue = N'%%') OR 
                (FullName LIKE @SearchValue) OR 
                (Email LIKE @SearchValue) OR 
                (Phone LIKE @SearchValue)";

            string countSql = $"SELECT COUNT(*) FROM Employees WHERE {condition}";
            string dataSql = $@"
                SELECT * FROM Employees 
                WHERE {condition} 
                ORDER BY FullName";

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
            var dataItems = await connection.QueryAsync<Employee>(dataSql, parameters);

            return new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Employees 
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking,
                    RoleNames = @RoleNames
                WHERE EmployeeID = @EmployeeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*) 
                FROM Employees 
                WHERE Email = @Email AND EmployeeID <> @EmployeeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
            return count == 0;
        }

        public async Task<bool> InUseEmailAsync(string email, int excludeEmployeeID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Employees
                WHERE LOWER(Email) = @Email
                AND EmployeeID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = (email ?? "").Trim().ToLowerInvariant(),
                ExcludeID = excludeEmployeeID
            });

            return count > 0;
        }

        public async Task<bool> InUsePhoneAsync(string phone, int excludeEmployeeID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*)
                FROM Employees
                WHERE Phone = @Phone
                AND EmployeeID <> @ExcludeID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Phone = (phone ?? "").Trim(),
                ExcludeID = excludeEmployeeID
            });

            return count > 0;
        }

        public async Task<bool> UpdateRolesAsync(int employeeID, string roleNames)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Employees
                SET RoleNames = @RoleNames
                WHERE EmployeeID = @EmployeeID";

            return await connection.ExecuteAsync(sql, new
            {
                EmployeeID = employeeID,
                RoleNames = roleNames
            }) > 0;
        }

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