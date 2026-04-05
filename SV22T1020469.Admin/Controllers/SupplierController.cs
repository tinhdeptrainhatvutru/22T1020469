using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020605.Admin.AppCodes;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SV22T1020605.Admin.Controllers
{
    [Authorize] // BẮT BUỘC ĐĂNG NHẬP
    public class SupplierController : Controller
    {
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH) ?? new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 20,
                SearchValue = ""
            };
            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
            var result = await PartnerDataService.ListSuppliersAsync(input);
            return PartialView("Search", result); // Dùng PartialView cho AJAX
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            ViewBag.Provinces = await SelectListHelper.Provinces();
            var data = new Supplier() { SupplierID = 0 };
            return View("Edit", data);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật nhà cung cấp";
            ViewBag.Provinces = await SelectListHelper.Provinces();

            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null) return RedirectToAction("Index");

            return View(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Supplier data)
        {
            if (data == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu gửi lên không hợp lệ.";
                return RedirectToAction("Index");
            }

            // CHUẨN HÓA DỮ LIỆU: Cắt khoảng trắng 2 đầu
            data.SupplierName = data.SupplierName?.Trim() ?? "";
            data.ContactName = data.ContactName?.Trim() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Email = data.Email?.Trim().ToLowerInvariant() ?? "";
            data.Address = data.Address?.Trim() ?? "";
            data.Province = data.Province?.Trim() ?? "";

            // VALIDATE DỮ LIỆU BẮT BUỘC
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");

            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");
            else
            {
                try { _ = new MailAddress(data.Email); }
                catch { ModelState.AddModelError("Email", "Email không hợp lệ"); }
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                int digits = data.Phone.Count(char.IsDigit);
                if (digits < 7 || digits > 20)
                    ModelState.AddModelError("Phone", "Số điện thoại phải có từ 7 đến 20 chữ số");
            }

            if (!string.IsNullOrWhiteSpace(data.Email))
            {
                bool inUseEmail = await PartnerDataService.InUseSupplierEmailAsync(data.Email, data.SupplierID);
                if (inUseEmail)
                    ModelState.AddModelError("Email", "Email này đã được sử dụng!");
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                bool inUsePhone = await PartnerDataService.InUseSupplierPhoneAsync(data.Phone, data.SupplierID);
                if (inUsePhone)
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng!");
            }

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");

            if (string.IsNullOrWhiteSpace(data.Address))
                ModelState.AddModelError(nameof(data.Address), "Địa chỉ không được để trống");

            // NẾU CÓ LỖI, TRẢ VỀ VIEW KÈM THÔNG BÁO VÀ LOAD LẠI DROPDOWN
            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit", data);
            }

            // LƯU DATABASE
            try
            {
                if (data.SupplierID == 0)
                {
                    int newId = await PartnerDataService.AddSupplierAsync(data);
                    if (newId <= 0)
                    {
                        ModelState.AddModelError("", "Không thể bổ sung nhà cung cấp vào CSDL.");
                        ViewBag.Title = "Bổ sung nhà cung cấp";
                        ViewBag.Provinces = await SelectListHelper.Provinces();
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Bổ sung nhà cung cấp thành công.";
                }
                else
                {
                    bool ok = await PartnerDataService.UpdateSupplierAsync(data);
                    if (!ok)
                    {
                        ModelState.AddModelError("", "Không tìm thấy nhà cung cấp để cập nhật.");
                        ViewBag.Title = "Cập nhật nhà cung cấp";
                        ViewBag.Provinces = await SelectListHelper.Provinces();
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công.";
                }
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu nhà cung cấp. Vui lòng thử lại.");
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit", data);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa nhà cung cấp";
            var supplier = await PartnerDataService.GetSupplierAsync(id);
            if (supplier == null) return RedirectToAction("Index");
            return View(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, bool confirm = true)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Mã nhà cung cấp không hợp lệ.";
                return RedirectToAction("Index");
            }

            try
            {
                bool inUse = await PartnerDataService.IsUsedSupplierAsync(id);
                if (inUse)
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này vì đã có dữ liệu mặt hàng liên quan!";
                    return RedirectToAction("Index");
                }

                bool deleted = await PartnerDataService.DeleteSupplierAsync(id);
                if (!deleted)
                {
                    TempData["ErrorMessage"] = "Xóa nhà cung cấp thất bại. Dữ liệu có thể đã thay đổi.";
                    return RedirectToAction("Index");
                }

                TempData["SuccessMessage"] = "Đã xóa nhà cung cấp thành công.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Hệ thống gặp lỗi khi xóa nhà cung cấp.";
            }

            return RedirectToAction("Index");
        }
    }
}