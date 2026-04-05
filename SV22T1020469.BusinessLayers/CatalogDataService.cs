using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.DataLayers.SQLServer;
using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020469.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến danh mục hàng hóa của hệ thống, 
    /// bao gồm: mặt hàng (Product), thuộc tính của mặt hàng (ProductAttribute) và ảnh của mặt hàng (ProductPhoto).
    /// </summary>
    public static class CatalogDataService
    {
        private static readonly IProductRepository productDB;
        private static readonly IGenericRepository<Category> categoryDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static CatalogDataService()
        {
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
            productDB = new ProductRepository(Configuration.ConnectionString);
        }

        #region Category
        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input)
        {
            return await categoryDB.ListAsync(input);
        }

        public static async Task<Category?> GetCategoryAsync(int categoryID)
        {
            return await categoryDB.GetAsync(categoryID);
        }

        public static async Task<int> AddCategoryAsync(Category data)
        {
            return await categoryDB.AddAsync(data);
        }

        public static async Task<bool> UpdateCategoryAsync(Category data)
        {
            return await categoryDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteCategoryAsync(int categoryID)
        {
            if (await categoryDB.IsUsed(categoryID)) return false;
            return await categoryDB.DeleteAsync(categoryID);
        }

        public static async Task<bool> IsUsedCategoryAsync(int categoryID)
        {
            return await categoryDB.IsUsed(categoryID);
        }
        #endregion

        #region Product
        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
        {
            return await productDB.ListAsync(input);
        }

        public static async Task<Product?> GetProductAsync(int productID)
        {
            return await productDB.GetAsync(productID);
        }

        public static async Task<int> AddProductAsync(Product data)
        {
            return await productDB.AddAsync(data);
        }

        public static async Task<bool> UpdateProductAsync(Product data)
        {
            return await productDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteProductAsync(int productID)
        {
            if (await productDB.IsUsedAsync(productID)) return false;
            return await productDB.DeleteAsync(productID);
        }

        public static async Task<bool> IsUsedProductAsync(int productID)
        {
            return await productDB.IsUsedAsync(productID);
        }

        public static async Task<int> CountProductsAsync()
        {
            return await productDB.CountAsync();
        }
        #endregion

        #region Product Photo
        public static async Task<List<ProductPhoto>> ListProductPhotosAsync(int productID)
        {
            return await productDB.ListPhotosAsync(productID);
        }

        public static async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            return await productDB.GetPhotoAsync(photoID);
        }

        public static async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            return await productDB.AddPhotoAsync(data);
        }

        public static async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            return await productDB.UpdatePhotoAsync(data);
        }

        public static async Task<bool> DeletePhotoAsync(long photoID)
        {
            return await productDB.DeletePhotoAsync(photoID);
        }
        #endregion

        #region Product Attribute
        public static async Task<List<ProductAttribute>> ListProductAttributesAsync(int productID)
        {
            return await productDB.ListAttributesAsync(productID);
        }

        public static async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            return await productDB.GetAttributeAsync(attributeID);
        }

        public static async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            return await productDB.AddAttributeAsync(data);
        }

        public static async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            return await productDB.UpdateAttributeAsync(data);
        }

        public static async Task<bool> UpdateAttributeQuantityAsync(long attributeID, int quantity)
        {
            return await productDB.UpdateAttributeQuantityAsync(attributeID, quantity);
        }

        public static async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            return await productDB.DeleteAttributeAsync(attributeID);
        }
        #endregion
    }
}