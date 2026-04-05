namespace SV22T1020469.Models.Sales
{
    public class OrderDetailViewInfo : OrderDetail
    {
        public new string ProductName { get; set; } = "";

        public string Unit { get; set; } = "";

        public string Photo { get; set; } = "";
    }
}