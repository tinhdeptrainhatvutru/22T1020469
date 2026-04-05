using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020469.BusinessLayers
{
    /// <summary>
    /// Lớp lưu giữ các thông tin cấu hình của ứng dụng cho BusinessLayer
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString="";
        /// <summary>
        /// khởi tạo cấu hình cho BusinessLayer
        /// (hàm này phải gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// Lấy chuỗi tham số kết nối đến CSDL sử dụng trong hệ thống
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
