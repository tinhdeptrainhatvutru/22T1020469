using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.DataDictionary;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý lấy danh mục Tỉnh/Thành từ SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

            var result = await connection.QueryAsync<Province>(sql);
            return result.ToList();
        }
    }
}