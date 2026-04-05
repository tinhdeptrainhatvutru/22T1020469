using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Security;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020469.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection =
                new SqlConnection(_connectionString);

            string sql = @"
                SELECT
                    EmployeeID AS UserId,
                    Email AS UserName,
                    FullName AS DisplayName,
                    Email,
                    Photo,
                    RoleNames
                FROM Employees
                WHERE Email = @Email
                AND Password = @Password
                AND IsWorking = 1";

            var parameters = new
            {
                Email = userName,
                Password = ToMD5(password)
            };

            return await connection
                .QueryFirstOrDefaultAsync<UserAccount>(
                    sql,
                    parameters
                );
        }

        public async Task<bool> ChangePasswordAsync(
            string userName,
            string password)
        {
            using var connection =
                new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees
                SET Password = @Password
                WHERE Email = @Email";

            var parameters = new
            {
                Email = userName,
                Password = ToMD5(password)
            };

            return await connection
                .ExecuteAsync(sql, parameters) > 0;
        }

        private string ToMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes =
                    md5.ComputeHash(
                        Encoding.UTF8.GetBytes(input));

                var sb = new StringBuilder();

                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
    }
}

