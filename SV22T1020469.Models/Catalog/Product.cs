namespace SV22T1020469.Models.Catalog
{
    /// <summary>
    /// Mặt hàng
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Mã mặt hàng
        /// </summary>
        public int ProductID { get; set; }

        /// <summary>
        /// Tên mặt hàng
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả mặt hàng
        /// </summary>
        public string? ProductDescription { get; set; }

        /// <summary>
        /// Mã nhà cung cấp
        /// </summary>
        public int? SupplierID { get; set; }

        /// <summary>
        /// Mã loại hàng
        /// </summary>
        public int? CategoryID { get; set; }

        /// <summary>
        /// Đơn vi tính
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Giá
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Số lượng tồn kho
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Tên file ảnh đại diện của mặt hàng (nếu có)
        /// </summary>
        public string? Photo { get; set; }

        /// <summary>
        /// Mặt hàng hiện có đang được bán hay không?
        /// </summary>
        public bool IsSelling { get; set; }

        // ==========================================
        // THÊM 2 THUỘC TÍNH NÀY ĐỂ HIỂN THỊ TRÊN VIEW
        // ==========================================

        /// <summary>
        /// Tên loại hàng (Dùng để hiển thị thay vì hiện CategoryID)
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Tên nhà cung cấp (Dùng để hiển thị thay vì hiện SupplierID)
        /// </summary>
        public string SupplierName { get; set; } = string.Empty;
    }
}