using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020605.Admin.AppCodes;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.HR;
using System.IO;
using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SV22T1020605.Admin.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";
        private readonly IWebHostEnvironment _environment;

        public EmployeeController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý nhân viên";

            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH)
                        ?? new PaginationSearchInput()
                        {
                            Page = 1,
                            PageSize = 20,
                            SearchValue = ""
                        };

            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);

            var result = await HRDataService.ListEmployeesAsync(input);

            return PartialView("Search", result);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            return View("Edit", new Employee());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee data)
        {
            data.EmployeeID = 0;
            return await Save(data, null);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = id == 0 ?
                "Bổ sung nhân viên" :
                "Cập nhật nhân viên";

            Employee? data;

            if (id == 0)
            {
                data = new Employee();
            }
            else
            {
                data = await HRDataService.GetEmployeeAsync(id);

                // FIX: tránh crash nếu id sai
                if (data == null)
                    return RedirectToAction("Index");
            }

            return View(data!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Employee data, IFormFile? uploadPhoto)
        {
            if (data == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Index");
            }

            bool isNew = data.EmployeeID == 0;

            data.FullName = data.FullName?.Trim() ?? "";
            data.Email = data.Email?.Trim().ToLowerInvariant() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Address = data.Address?.Trim() ?? "";
            data.Photo = data.Photo?.Trim();
            data.RoleNames = data.RoleNames?.Trim();

            if (string.IsNullOrWhiteSpace(data.FullName))
                ModelState.AddModelError("FullName", "Tên nhân viên không được rỗng");

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
                bool inUseEmail = await HRDataService.InUseEmployeeEmailAsync(data.Email, data.EmployeeID);
                if (inUseEmail)
                    ModelState.AddModelError("Email", "Email này đã được sử dụng!");
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                bool inUsePhone = await HRDataService.InUseEmployeePhoneAsync(data.Phone, data.EmployeeID);
                if (inUsePhone)
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng!");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.EmployeeID == 0 ?
                    "Bổ sung nhân viên" :
                    "Cập nhật nhân viên";

                return View("Edit", data);
            }

            // Xử lý ảnh (gán mặc định / giữ lại ảnh cũ khi không upload)
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
                    string filePath = Path.Combine(_environment.WebRootPath, "images", "employees", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }

                    data.Photo = fileName;
                }
            }
            else
            {
                if (isNew && string.IsNullOrWhiteSpace(data.Photo))
                    data.Photo = "nophoto.svg";

                if (!isNew && string.IsNullOrWhiteSpace(data.Photo))
                {
                    var existing = await HRDataService.GetEmployeeAsync(data.EmployeeID);
                    data.Photo = existing?.Photo?.Trim();

                    if (string.IsNullOrWhiteSpace(data.Photo))
                        data.Photo = "nophoto.svg";
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật nhân viên";
                return View("Edit", data);
            }

            try
            {
                if (data.EmployeeID == 0)
                {
                    int newId = await HRDataService.AddEmployeeAsync(data);
                    if (newId <= 0)
                    {
                        ModelState.AddModelError("", "Không thể bổ sung nhân viên.");
                        ViewBag.Title = "Bổ sung nhân viên";
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Bổ sung nhân viên thành công.";
                }
                else
                {
                    bool ok = await HRDataService.UpdateEmployeeAsync(data);
                    if (!ok)
                    {
                        ModelState.AddModelError("", "Không tìm thấy nhân viên để cập nhật.");
                        ViewBag.Title = "Cập nhật nhân viên";
                        return View("Edit", data);
                    }
                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công.";
                }
            }
            catch
            {
                ModelState.AddModelError("", "Hệ thống gặp lỗi khi lưu dữ liệu nhân viên.");
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật nhân viên";
                return View("Edit", data);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var data = await HRDataService.GetEmployeeAsync(id);

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
                TempData["ErrorMessage"] = "Mã nhân viên không hợp lệ.";
                return RedirectToAction("Index");
            }

            try
            {
                if (await HRDataService.DeleteEmployeeAsync(id))
                {
                    TempData["SuccessMessage"] = "Xóa nhân viên thành công.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Không thể xóa nhân viên vì có dữ liệu liên quan.");
                var data = await HRDataService.GetEmployeeAsync(id);
                return View(data);
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể xóa vì dữ liệu đang được sử dụng ở nơi khác.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            if (id <= 0) return RedirectToAction("Index");
            var data = await HRDataService.GetEmployeeAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var data = await HRDataService.GetEmployeeAsync(id);
            if (data == null)
            {
                TempData["ErrorMessage"] = "Nhân viên không tồn tại.";
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
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError("", "Nhân viên chưa có email hợp lệ.");

            if (!ModelState.IsValid)
                return View(data);

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

            TempData["SuccessMessage"] = "Đổi mật khẩu nhân viên thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            if (id <= 0) return RedirectToAction("Index");
            var data = await HRDataService.GetEmployeeAsync(id);
            if (data == null) return RedirectToAction("Index");

            var selectedRoles = (data.RoleNames ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            ViewBag.SelectedRoles = selectedRoles;
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, string[] role)
        {
            var data = await HRDataService.GetEmployeeAsync(id);
            if (data == null)
            {
                TempData["ErrorMessage"] = "Nhân viên không tồn tại.";
                return RedirectToAction("Index");
            }

            var validRoles = new[] { "product", "order", "admin" };
            var selected = (role ?? Array.Empty<string>())
                .Where(r => validRoles.Contains((r ?? "").Trim(), StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Trim().ToLowerInvariant())
                .Distinct()
                .ToArray();

            string roleNames = string.Join(";", selected);
            bool ok = await HRDataService.UpdateEmployeeRolesAsync(id, roleNames);
            if (!ok)
            {
                ModelState.AddModelError("", "Không thể cập nhật phân quyền.");
                ViewBag.SelectedRoles = selected.ToList();
                return View(data);
            }

            TempData["SuccessMessage"] = "Cập nhật phân quyền thành công.";
            return RedirectToAction("Index");
        }
    }
}

