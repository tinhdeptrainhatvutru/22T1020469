namespace SV22T1020469.Shop.Models
{
    /// <summary>
    /// Thông tin khách hàng lưu trong Session sau khi đăng nhập
    /// </summary>
    public class WebUserData
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Province { get; set; } = "";
    }
}