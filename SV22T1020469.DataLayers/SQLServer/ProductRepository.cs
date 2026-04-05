using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Common;

namespace SV22T1020469.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho Mặt hàng, Thuộc tính và Hình ảnh trên SQL Server
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==========================================================
        // 1. QUẢN LÝ MẶT HÀNG (PRODUCTS)
        // ==========================================================

        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Products (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Quantity, Photo, IsSelling)
                VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Quantity, @Photo, @IsSelling);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                data.ProductName,
                data.ProductDescription,
                data.SupplierID,
                data.CategoryID,
                data.Unit,
                data.Price,
                data.Quantity,
                data.Photo,
                data.IsSelling
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Theo logic thông thường, khi xóa 1 mặt hàng, ta phải xóa các ảnh và thuộc tính của nó trước 
            // để tránh lỗi khóa ngoại.
            string sql = @"
                DELETE FROM ProductPhotos WHERE ProductID = @ProductID;
                DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                DELETE FROM Products WHERE ProductID = @ProductID;";

            var parameters = new { ProductID = id };

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<Product?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = id });
        }

        // ĐÃ SỬA LỖI Ở ĐÂY: Đổi tên hàm thành IsUsedAsync
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Một mặt hàng bị coi là "đang sử dụng" (không được xóa) nếu nó đã từng xuất hiện trong đơn hàng (OrderDetails)
            string sql = @"
                IF EXISTS (SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID)
                    SELECT 1;
                ELSE
                    SELECT 0;";

            return await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = id });
        }

        /// <summary>
        /// Hàm này để thỏa mãn IGenericRepository, sẽ gọi sang bản có ProductSearchInput
        /// </summary>
        public Task<PagedResult<Product>> ListAsync(PaginationSearchInput input)
        {
            return ListAsync((ProductSearchInput)input);
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            string searchValue = $"%{input.SearchValue}%";

            // Điều kiện lọc dữ liệu cho Product
            string condition = @"
                (@SearchValue = N'%%' OR ProductName LIKE @SearchValue)
                AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                AND (Price >= @MinPrice)
                AND (@MaxPrice = 0 OR Price <= @MaxPrice)";

            string countSql = $"SELECT COUNT(*) FROM Products WHERE {condition}";

            string dataSql = $@"
                SELECT * FROM Products 
                WHERE {condition} 
                ORDER BY ProductName";

            if (input.PageSize > 0)
            {
                dataSql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            var parameters = new
            {
                SearchValue = searchValue,
                CategoryID = input.CategoryID,
                SupplierID = input.SupplierID,
                MinPrice = input.MinPrice,
                MaxPrice = input.MaxPrice,
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var dataItems = await connection.QueryAsync<Product>(dataSql, parameters);

            return new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Products 
                SET ProductName = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID = @SupplierID,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    Price = @Price,
                    Quantity = @Quantity,
                    Photo = @Photo,
                    IsSelling = @IsSelling
                WHERE ProductID = @ProductID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        public async Task<int> CountAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");
        }

        // ==========================================================
        // 2. QUẢN LÝ THUỘC TÍNH (ATTRIBUTES)
        // ==========================================================

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder, Quantity)
                VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder, @Quantity);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
            return await connection.ExecuteAsync(sql, new { AttributeID = attributeID }) > 0;
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
        }

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
            var result = await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID });
            return result.ToList();
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductAttributes 
                SET AttributeName = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder = @DisplayOrder,
                    Quantity = @Quantity
                WHERE AttributeID = @AttributeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> UpdateAttributeQuantityAsync(long attributeID, int quantity)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductAttributes
                SET Quantity = @Quantity
                WHERE AttributeID = @AttributeID";
            return await connection.ExecuteAsync(sql, new { AttributeID = attributeID, Quantity = quantity }) > 0;
        }

        // ==========================================================
        // 3. QUẢN LÝ HÌNH ẢNH (PHOTOS)
        // ==========================================================

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
            return await connection.ExecuteAsync(sql, new { PhotoID = photoID }) > 0;
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
        }

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
            var result = await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID });
            return result.ToList();
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductPhotos 
                SET Photo = @Photo,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden = @IsHidden
                WHERE PhotoID = @PhotoID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }
    }
}