using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020605.Admin.AppCodes;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Common;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020605.Admin.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ProductController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<IActionResult> Index(int categoryID = 0, int supplierID = 0, string searchValue = "", int page = 1)
        {
            ViewBag.Title = "Quản lý Mặt hàng";
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.SupplierID = supplierID;

            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = 20,
                SearchValue = searchValue?.Trim() ?? "",
                CategoryID = categoryID,
                SupplierID = supplierID
            };

            var data = await CatalogDataService.ListProductsAsync(input);
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();
            return View("Edit", new Product() { ProductID = 0, IsSelling = true });
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin mặt hàng";
            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            // ĐÃ FIX: Đổi thành ListProductPhotosAsync và ListProductAttributesAsync
            ViewBag.Photos = await CatalogDataService.ListProductPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListProductAttributesAsync(id);

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Product data, IFormFile? uploadPhoto)
        {
            bool isNew = data.ProductID == 0;

            string NormalizePhoto(string? photo)
            {
                if (string.IsNullOrWhiteSpace(photo)) return "";
                var p = photo.Trim().Replace("\\", "/");

                // Loại bỏ các tiền tố không cần thiết để DB lưu dạng tương đối dưới thư mục /images
                if (p.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(2);
                if (p.StartsWith("/"))
                    p = p.Substring(1);
                if (p.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                    p = p.Substring("images/".Length);

                return p;
            }

            data.ProductName = data.ProductName?.Trim() ?? "";
            data.Unit = data.Unit?.Trim() ?? "";
            data.ProductDescription = data.ProductDescription?.Trim();
            data.Photo = NormalizePhoto(data.Photo);

            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");

            if (data.CategoryID == null || data.CategoryID == 0)
                ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

            if (data.SupplierID == null || data.SupplierID == 0)
                ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

            if (string.IsNullOrWhiteSpace(data.Unit))
                ModelState.AddModelError(nameof(data.Unit), "Đơn vị tính không được để trống");

            if (data.Price <= 0)
                ModelState.AddModelError(nameof(data.Price), "Giá mặt hàng phải lớn hơn 0");

            if (data.Quantity < 0)
                ModelState.AddModelError(nameof(data.Quantity), "Số lượng tồn kho không được âm");

            if (uploadPhoto != null)
            {
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                var extension = Path.GetExtension(uploadPhoto.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Photo", "Chỉ hỗ trợ upload file ảnh (.png, .jpg, .jpeg, .gif)");
                }
                else if (uploadPhoto.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Photo", "Dung lượng ảnh không được vượt quá 5MB");
                }
                else
                {
                    string fileName = $"{DateTime.Now.Ticks}_{Guid.NewGuid()}{extension}";
                    string filePath = Path.Combine(_environment.WebRootPath, "images", "products", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    // Lưu dạng tương đối dưới thư mục /images => products/ten-file.jpg
                    data.Photo = $"products/{fileName}";
                }
            }
            else
            {
                // Nếu tạo mới mà không upload ảnh => bắt buộc set mặc định
                if (isNew && string.IsNullOrWhiteSpace(data.Photo))
                    data.Photo = "nophoto.svg";

                // Nếu edit mà không upload ảnh mới => giữ ảnh cũ (hoặc set mặc định nếu thiếu dữ liệu)
                if (!isNew && string.IsNullOrWhiteSpace(data.Photo))
                {
                    var existing = await CatalogDataService.GetProductAsync(data.ProductID);
                    data.Photo = NormalizePhoto(existing?.Photo);
                    if (string.IsNullOrWhiteSpace(data.Photo))
                        data.Photo = "nophoto.svg";
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();

                if (data.ProductID > 0)
                {
                    // ĐÃ FIX
                    ViewBag.Photos = await CatalogDataService.ListProductPhotosAsync(data.ProductID);
                    ViewBag.Attributes = await CatalogDataService.ListProductAttributesAsync(data.ProductID);
                }
                return View("Edit", data);
            }

            try
            {
                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                TempData["SuccessMessage"] = "Đã lưu thông tin mặt hàng thành công.";
            }
            catch (SqlException ex)
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();

                if (data.ProductID > 0)
                {
                    ViewBag.Photos = await CatalogDataService.ListProductPhotosAsync(data.ProductID);
                    ViewBag.Attributes = await CatalogDataService.ListProductAttributesAsync(data.ProductID);
                }

                if (ex.Message.Contains("Invalid column name 'Quantity'", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("", "Không thể lưu tồn kho ở bảng Products vì CSDL chưa có cột Quantity. Nếu bạn quản lý tồn kho theo phân loại (màu/kích cỡ) thì hãy cập nhật ở phần Thuộc tính. Nếu cần lưu theo sản phẩm, hãy chạy script scripts/Add-Product-Quantity.sql.");
                }
                else
                {
                    ModelState.AddModelError("", "Không thể lưu dữ liệu sản phẩm do lỗi cơ sở dữ liệu. Vui lòng thử lại.");
                }

                return View("Edit", data);
            }
            catch
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();

                if (data.ProductID > 0)
                {
                    ViewBag.Photos = await CatalogDataService.ListProductPhotosAsync(data.ProductID);
                    ViewBag.Attributes = await CatalogDataService.ListProductAttributesAsync(data.ProductID);
                }

                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật tồn kho. Vui lòng thử lại.");
                return View("Edit", data);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await CatalogDataService.DeleteProductAsync(id);
                    TempData["SuccessMessage"] = "Xóa mặt hàng thành công.";
                    return RedirectToAction("Index");
                }
                catch
                {
                    TempData["ErrorMessage"] = "Không thể xóa vì dữ liệu đang được sử dụng ở nơi khác.";
                    return RedirectToAction("Index");
                }
            }

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            return View(product);
        }

        public async Task<IActionResult> Photo(int id = 0, string method = "", int photoId = 0)
        {
            switch (method)
            {
                case "add":
                    ViewBag.Title = "Bổ sung ảnh cho mặt hàng";
                    return View("EditPhoto", new ProductPhoto() { ProductID = id, PhotoID = 0, IsHidden = false });
                case "edit":
                    ViewBag.Title = "Cập nhật ảnh mặt hàng";
                    var photo = await CatalogDataService.GetPhotoAsync(photoId);
                    if (photo == null) return RedirectToAction("Edit", new { id = id });
                    return View("EditPhoto", photo);
                case "delete":
                    ViewBag.Title = "Xóa ảnh mặt hàng";
                    var deletePhoto = await CatalogDataService.GetPhotoAsync(photoId);
                    if (deletePhoto == null) return RedirectToAction("Edit", new { id = id });
                    return View("DeletePhoto", deletePhoto);
                default:
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            data.Description = data.Description?.Trim() ?? "";

            string NormalizePhoto(string? photo)
            {
                if (string.IsNullOrWhiteSpace(photo)) return "";
                var p = photo.Trim().Replace("\\", "/");

                if (p.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(2);
                if (p.StartsWith("/"))
                    p = p.Substring(1);
                if (p.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                    p = p.Substring("images/".Length);

                // Nếu ảnh placeholder (không nằm trong /products) thì giữ nguyên
                if (p.StartsWith("nophoto.", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p, "nophoto.svg", StringComparison.OrdinalIgnoreCase))
                    return p;

                if (!p.StartsWith("products/", StringComparison.OrdinalIgnoreCase))
                    p = $"products/{p}";

                return p;
            }

            data.Photo = NormalizePhoto(data.Photo);

            if (uploadPhoto != null)
            {
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                var extension = Path.GetExtension(uploadPhoto.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Photo", "Chỉ hỗ trợ file ảnh (.png, .jpg, .jpeg, .gif)");
                }
                else
                {
                    string fileName = $"{DateTime.Now.Ticks}_{Guid.NewGuid()}{extension}";
                    string filePath = Path.Combine(_environment.WebRootPath, "images", "products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = $"products/{fileName}";
                }
            }

            if (data.PhotoID == 0 && string.IsNullOrEmpty(data.Photo))
                ModelState.AddModelError("Photo", "Vui lòng chọn ảnh để upload");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh cho mặt hàng" : "Cập nhật ảnh mặt hàng";
                return View("EditPhoto", data);
            }

            if (data.PhotoID == 0) await CatalogDataService.AddPhotoAsync(data);
            else await CatalogDataService.UpdatePhotoAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int photoId, int productId)
        {
            await CatalogDataService.DeletePhotoAsync(photoId);
            return RedirectToAction("Edit", new { id = productId });
        }

        public async Task<IActionResult> Attribute(int id = 0, string method = "", int attributeId = 0)
        {
            switch (method)
            {
                case "add":
                    ViewBag.Title = "Bổ sung thuộc tính";
                    return View("EditAttribute", new ProductAttribute() { ProductID = id, AttributeID = 0 });
                case "edit":
                    ViewBag.Title = "Thay đổi thuộc tính";
                    var attr = await CatalogDataService.GetAttributeAsync(attributeId);
                    if (attr == null) return RedirectToAction("Edit", new { id = id });
                    return View("EditAttribute", attr);
                case "delete":
                    ViewBag.Title = "Xóa thuộc tính";
                    var deleteAttr = await CatalogDataService.GetAttributeAsync(attributeId);
                    if (deleteAttr == null) return RedirectToAction("Edit", new { id = id });
                    return View("DeleteAttribute", deleteAttr);
                default:
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            data.AttributeName = data.AttributeName?.Trim() ?? "";
            data.AttributeValue = data.AttributeValue?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");

            if (data.Quantity < 0)
                ModelState.AddModelError(nameof(data.Quantity), "Số lượng tồn kho không được âm");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Thay đổi thuộc tính";
                return View("EditAttribute", data);
            }

            try
            {
                if (data.AttributeID == 0) await CatalogDataService.AddAttributeAsync(data);
                else await CatalogDataService.UpdateAttributeAsync(data);
            }
            catch
            {
                ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Thay đổi thuộc tính";
                ModelState.AddModelError("", "Không thể lưu tồn kho biến thể. Vui lòng kiểm tra dữ liệu và thử lại.");
                return View("EditAttribute", data);
            }

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttribute(int attributeId, int productId)
        {
            await CatalogDataService.DeleteAttributeAsync(attributeId);
            return RedirectToAction("Edit", new { id = productId });
        }
    }
}