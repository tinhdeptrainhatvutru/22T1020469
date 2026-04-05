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
    [Authorize]
    public class CustomerController : Controller
    {
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý khách hàng";

            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH)
                        ?? new PaginationSearchInput()
                        {
                            Page = 1,
                            PageSize = 20,
                            SearchValue = ""
                        };

            return View(input);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            ViewBag.Provinces = await SelectListHelper.Provinces();
            return View("Edit", new Customer { CustomerID = 0 });
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);

            var result = await PartnerDataService.ListCustomersAsync(input);

            return PartialView("Search", result);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = id == 0 ?
                "Bổ sung khách hàng" :
                "Cập nhật khách hàng";
            ViewBag.Provinces = await SelectListHelper.Provinces();

            Customer? data;

            if (id == 0)
            {
                data = new Customer();
            }
            else
            {
                data = await PartnerDataService.GetCustomerAsync(id);

                // FIX: tránh crash khi id không tồn tại
                if (data == null)
                    return RedirectToAction("Index");
            }

            return View(data!);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Customer data)
        {
            if (data == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu gửi lên không hợp lệ!";
                return RedirectToAction("Index");
            }

            data.CustomerName = data.CustomerName?.Trim() ?? "";
            data.ContactName = data.ContactName?.Trim() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Email = data.Email?.Trim().ToLowerInvariant() ?? "";
            data.Province = data.Province?.Trim() ?? "";
            data.Address = data.Address?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError("CustomerName", "Tên khách hàng không được rỗng");

            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError("ContactName", "Tên giao dịch không được rỗng");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError("Phone", "Điện thoại không được rỗng");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError("Email", "Email không được rỗng");
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
                bool inUseEmail = await PartnerDataService.InUseCustomerEmailAsync(data.Email, data.CustomerID);
                if (inUseEmail)
                    ModelState.AddModelError("Email", "Email này đã được sử dụng!");
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                bool inUsePhone = await PartnerDataService.InUseCustomerPhoneAsync(data.Phone, data.CustomerID);
                if (inUsePhone)
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng!");
            }

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError("Province", "Vui lòng chọn Tỉnh/Thành");

            if (string.IsNullOrWhiteSpace(data.Address))
                ModelState.AddModelError("Address", "Địa chỉ không được rỗng");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CustomerID == 0 ?
                    "Bổ sung khách hàng" :
                    "Cập nhật khách hàng";
                ViewBag.Provinces = await SelectListHelper.Provinces();

                return View("Edit", data);
            }

            try
            {
                if (data.CustomerID == 0)
                {
                    int newId = await PartnerDataService.AddCustomerAsync(data);
                    if (newId <= 0)
                    {
                        ModelState.AddModelError("", "Không thể bổ sung khách hàng vào CSDL.");
                        ViewBag.Title = "Bổ sung khách hàng";
                        ViewBag.Provinces = await SelectListHelper.Provinces();
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Bổ sung khách hàng thành công.";
                }
                else
                {
                    bool ok = await PartnerDataService.UpdateCustomerAsync(data);
                    if (!ok)
                    {
                        ModelState.AddModelError("", "Không tìm thấy khách hàng để cập nhật.");
                        ViewBag.Title = "Cập nhật khách hàng";
                        ViewBag.Provinces = await SelectListHelper.Provinces();
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật khách hàng thành công.";
                }
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu khách hàng. Vui lòng thử lại.");
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật khách hàng";
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit", data);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var data = await PartnerDataService.GetCustomerAsync(id);

            // FIX: tránh crash
            if (data == null)
                return RedirectToAction("Index");

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, bool confirm = true)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Mã khách hàng không hợp lệ.";
                return RedirectToAction("Index");
            }

            try
            {
                bool isUsed = await PartnerDataService.IsUsedCustomerAsync(id);
                if (isUsed)
                {
                    TempData["ErrorMessage"] = "Không thể xóa khách hàng này vì đã có dữ liệu đơn hàng liên quan.";
                    return RedirectToAction("Index");
                }

                bool deleted = await PartnerDataService.DeleteCustomerAsync(id);
                if (!deleted)
                {
                    TempData["ErrorMessage"] = "Xóa khách hàng thất bại. Dữ liệu có thể đã thay đổi.";
                    return RedirectToAction("Index");
                }

                TempData["SuccessMessage"] = "Đã xóa khách hàng thành công.";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "Hệ thống gặp lỗi khi xóa khách hàng.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var data = await PartnerDataService.GetCustomerAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var data = await PartnerDataService.GetCustomerAsync(id);
            if (data == null)
            {
                TempData["ErrorMessage"] = "Khách hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            newPassword = newPassword?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới.");

            if (newPassword.Length < 6)
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");

            if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
                return View(data);

            if (string.IsNullOrWhiteSpace(data.Email))
            {
                ModelState.AddModelError("", "Khách hàng chưa có email đăng nhập hợp lệ.");
                return View(data);
            }

            try
            {
                bool ok = await UserAccountDataService.ChangePasswordAsync(data.Email.Trim(), newPassword);
                if (!ok)
                {
                    ModelState.AddModelError("", "Đổi mật khẩu thất bại.");
                    return View(data);
                }
            }
            catch
            {
                ModelState.AddModelError("", "Hệ thống gặp lỗi khi đổi mật khẩu.");
                return View(data);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu khách hàng thành công.";
            return RedirectToAction("Index");
        }
    }
}

