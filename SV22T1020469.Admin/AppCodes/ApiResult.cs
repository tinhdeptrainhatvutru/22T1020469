namespace SV22T1020605.Admin
{
    /// <summary>
    /// Biểu diễn dữ liệu trả về của các API
    /// </summary>
    public class ApiResult
    {
        public ApiResult(int code, string message = "" )
        {
            Code = code;
            Message = message;
        }
        /// <summary>
        /// Mã kết quả trả về (qui ước 0 tức là lỗi hoặc không thành công
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Thông báo lỗi (nếu có)
        /// </summary>
        public string Message { get; set; } = "";
    }
}
