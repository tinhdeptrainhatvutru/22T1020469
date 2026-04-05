using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.HR;
using SV22T1020469.Models.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SV22T1020605.Admin.Controllers
{
    [Authorize]   // toàn bộ Admin phải đăng nhập
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.UserName = username;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Errol", "nhập tên và mật khẩu");
                return View();
            }
            string hashePassword = CryptHelper.HashMD5(password);
            //TODO: lấy thông tin tài khoản dựa vào tên đăng nhập và mật khẩu


            var userAccount = await UserAccountDataService.AuthorizeAsync(username, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại");
                return View();
            }
            //thông tin đăng nhập hợp lệ
            //Chuẩn bị thông tin mà sẽ ghi lên "giấy chứng nhận"
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames.Split(',').ToList()
            };
            //tạo ra giấy chứng nhận (ClaimsPrincipal)
            var principal = userData.CreatePrincipal();
            //trao giấy chứng nhận phía client
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Index", "Home");
        }

        // ==================== ĐĂNG KÝ (NHÂN VIÊN / ADMIN) ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new Employee());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Employee data, string password, string confirmPassword)
        {
            data ??= new Employee();
            data.FullName = data.FullName?.Trim() ?? "";
            data.Email = data.Email?.Trim().ToLowerInvariant() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Address = data.Address?.Trim();
            password = password?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(data.FullName))
                ModelState.AddModelError("FullName", "Họ và tên không được để trống!");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError("Email", "Email không được để trống!");
            else
            {
                try { _ = new MailAddress(data.Email); }
                catch { ModelState.AddModelError("Email", "Email không hợp lệ!"); }
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                int digits = data.Phone.Count(char.IsDigit);
                if (digits < 7 || digits > 20)
                    ModelState.AddModelError("Phone", "Số điện thoại phải có từ 7 đến 20 chữ số!");
            }

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("Password", "Mật khẩu không được để trống!");
            else if (password.Length < 6)
                ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự!");

            if (password != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp!");

            if (!ModelState.IsValid)
                return View(data);

            // Uniqueness
            if (await HRDataService.InUseEmployeeEmailAsync(data.Email, 0))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng!");
                return View(data);
            }

            if (!string.IsNullOrWhiteSpace(data.Phone) && await HRDataService.InUseEmployeePhoneAsync(data.Phone, 0))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng!");
                return View(data);
            }

            // Tạo nhân viên tối thiểu để có thể đăng nhập Admin
            var employee = new Employee
            {
                FullName = data.FullName,
                BirthDate = data.BirthDate,
                Address = data.Address,
                Phone = data.Phone,
                Email = data.Email,
                Photo = string.IsNullOrWhiteSpace(data.Photo) ? "nophoto.svg" : data.Photo?.Trim(),
                IsWorking = true,
                RoleNames = string.IsNullOrWhiteSpace(data.RoleNames) ? "admin" : data.RoleNames?.Trim()
            };

            int newId;
            try
            {
                newId = await HRDataService.AddEmployeeWithPasswordAsync(employee, password);
            }
            catch
            {
                ModelState.AddModelError("", "Đăng ký thất bại do lỗi hệ thống.");
                return View(data);
            }

            if (newId <= 0)
            {
                ModelState.AddModelError("", "Đăng ký thất bại, vui lòng thử lại!");
                return View(data);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // ==================== QUÊN MẬT KHẨU ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, string newPassword, string confirmPassword)
        {
            email = email?.Trim().ToLowerInvariant() ?? "";
            newPassword = newPassword?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("Email", "Vui lòng nhập email.");
            else
            {
                try { _ = new MailAddress(email); }
                catch { ModelState.AddModelError("Email", "Email không hợp lệ."); }
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            if (newPassword != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
                return View();

            bool changed;
            try
            {
                string newHashed = CryptHelper.HashMD5(newPassword);

                changed = await UserAccountDataService.ChangePasswordAsync(email, newHashed);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể đặt lại mật khẩu do lỗi hệ thống.");
                return View();
            }

            if (!changed)
            {
                ModelState.AddModelError("Email", "Email không tồn tại hoặc tài khoản không khả dụng.");
                return View();
            }

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập ngay.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout(string? message = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = message ?? "Bạn đã đăng xuất thành công. Vui lòng đăng nhập lại.";

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu không khớp!");
                return View();
            }

            var username = User.Identity?.Name ?? "";



            // kiểm tra mật khẩu cũ
            var check = await UserAccountDataService.AuthorizeAsync(username, oldPassword);
            if (check == null)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng!");
                return View();
            }

            // cập nhật mật khẩu
            bool ok = await UserAccountDataService.ChangePasswordAsync(username, newPassword);
            if (!ok)
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại!");
                return View();
            }

            return RedirectToAction("Logout", new
            {
                message = "Bạn đã đổi mật khẩu thành công. Vui lòng đăng nhập lại."
            });
        }
    }
}
