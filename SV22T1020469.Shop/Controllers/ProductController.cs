using Microsoft.AspNetCore.Mvc;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020469.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        public async Task<IActionResult> Index(
            string searchValue = "",
            int categoryID = 0,
            decimal minPrice = 0,
            decimal maxPrice = 0,
            string sortBy = "newest",
            bool onlyInStock = false,
            int pageSize = PAGE_SIZE,
            int page = 1)
        {
            bool isAjax = string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            // Load danh sách loại hàng cho filter (chỉ cần khi render full page)
            if (!isAjax)
            {
                var categories = await CatalogDataService.ListCategoriesAsync(
                    new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });
                ViewBag.Categories = categories.DataItems;
            }

            // Lưu lại filter để hiển thị trên form
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;
            ViewBag.OnlyInStock = onlyInStock;
            ViewBag.PageSize = pageSize;

            if (pageSize != 8 && pageSize != 12 && pageSize != 24 && pageSize != 36)
                pageSize = PAGE_SIZE;

            var input = new ProductSearchInput
            {
                Page = 1,
                PageSize = 0,
                SearchValue = searchValue,
                CategoryID = categoryID,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var allData = await CatalogDataService.ListProductsAsync(input);
            var products = allData.DataItems.AsEnumerable();

            if (onlyInStock)
                products = products.Where(p => p.Quantity > 0);

            products = sortBy switch
            {
                "name_asc" => products.OrderBy(p => p.ProductName),
                "name_desc" => products.OrderByDescending(p => p.ProductName),
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "stock_desc" => products.OrderByDescending(p => p.Quantity).ThenBy(p => p.ProductName),
                _ => products.OrderByDescending(p => p.ProductID)
            };

            var filtered = products.ToList();
            var totalRows = filtered.Count;
            var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
            if (totalPages <= 0) totalPages = 1;
            page = Math.Max(1, Math.Min(page, totalPages));

            var pageItems = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var data = new PagedResult<Product>
            {
                Page = page,
                PageSize = pageSize,
                RowCount = totalRows,
                DataItems = pageItems
            };

            if (isAjax)
                return PartialView("_ProductGrid", data);

            return View(data);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            // Load ảnh bổ sung nếu có
            var photos = await CatalogDataService.ListProductPhotosAsync(id);
            ViewBag.Photos = photos;

            // Load thuộc tính
            var attrs = await CatalogDataService.ListProductAttributesAsync(id);
            ViewBag.Attributes = attrs;

            return View(product);
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Stock(int productId)
        {
            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            return Json(new { success = true, quantity = product.Quantity });
        }
    }
}
