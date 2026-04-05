using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020605.Admin.AppCodes;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Catalog;
using SV22T1020469.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020605.Admin.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private const string CATEGORY_SEARCH = "CategorySearchInput";

        public IActionResult Index()
        {
            // Lấy trạng thái tìm kiếm từ Session, nếu chưa có thì khởi tạo mặc định
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH) ?? new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 20,
                SearchValue = ""
            };
            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);
            // Lưu lại trạng thái tìm kiếm vào Session
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
            return View(result); // Tương đương PartialView vì trong Search.cshtml đã cấu hình Layout = null
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var data = new Category() { CategoryID = 0 };
            return View("Edit", data);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Category data)
        {
            // 1. CHUẨN HÓA DỮ LIỆU (TRIM)
            data.CategoryName = data.CategoryName?.Trim() ?? "";
            data.Description = data.Description?.Trim() ?? "";

            // 2. KIỂM TRA ĐẦU VÀO
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật loại hàng";
                return View("Edit", data);
            }

            // 3. LƯU DỮ LIỆU
            if (data.CategoryID == 0)
            {
                await CatalogDataService.AddCategoryAsync(data);
                TempData["SuccessMessage"] = "Thêm loại hàng thành công!";
            }
            else
            {
                await CatalogDataService.UpdateCategoryAsync(data);
                TempData["SuccessMessage"] = "Cập nhật loại hàng thành công!";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id = 0)
        {
            ViewBag.Title = "Xóa loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Category data)
        {
            // Kiểm tra ràng buộc dữ liệu trước khi xóa
            if (await CatalogDataService.IsUsedCategoryAsync(data.CategoryID))
            {
                TempData["ErrorMessage"] = "Không thể xóa loại hàng này vì đang có mặt hàng liên quan!";
                return RedirectToAction("Index");
            }

            try
            {
                await CatalogDataService.DeleteCategoryAsync(data.CategoryID);
                TempData["SuccessMessage"] = "Xóa loại hàng thành công!";
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể xóa vì dữ liệu đang được sử dụng ở nơi khác.";
            }
            return RedirectToAction("Index");
        }
    }
}