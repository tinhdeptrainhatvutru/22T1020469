using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Partner;
using SV22T1020469.Shop.Models;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SV22T1020469.Shop.Controllers
{
    public class AccountController : Controller
    {
        private static readonly string[] LoginReasonWhitelist = { "cart", "viewcart", "checkout", "orders" };

        private static string? NormalizeLoginReason(string? reason)
        {
            if (string.IsNullOrEmpty(reason)) return null;
            foreach (var allowed in LoginReasonWhitelist)
            {
                if (string.Equals(reason, allowed, StringComparison.OrdinalIgnoreCase))
                    return allowed;
            }
            return null;
        }

        private WebUserData? CurrentUser =>
            HttpContext.Session.GetObject<WebUserData>("user");

        // ==================== ĐĂNG NHẬP ====================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (CurrentUser != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.PrefillEmail = TempData["LoginEmail"] as string;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null, string? reason = null)
        {
            email = email?.Trim() ?? "";
            password = password?.Trim() ?? "";
            var reasonSafe = NormalizeLoginReason(reason);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ email và mật khẩu!";
                TempData["LoginEmail"] = email;
                return RedirectToAction("Login", new { returnUrl, reason = reasonSafe });
            }

            var userAccount = await UserAccountDataService.AuthorizeAsync(email.ToLowerInvariant(), password);
            if (userAccount == null)
            {
                TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác!";
                TempData["LoginEmail"] = email;
                return RedirectToAction("Login", new { returnUrl, reason = reasonSafe });
            }

            // Chỉ cho phép Customer đăng nhập (RoleNames rỗng = Customer)
            if (!string.IsNullOrEmpty(userAccount.RoleNames))
            {
                TempData["ErrorMessage"] = "Tài khoản này không có quyền đăng nhập vào Shop!";
                TempData["LoginEmail"] = email;
                return RedirectToAction("Login", new { returnUrl, reason = reasonSafe });
            }

            // Lấy thêm thông tin đầy đủ từ Customer table
            int customerID = Convert.ToInt32(userAccount.UserId);
            var customer = await PartnerDataService.GetCustomerAsync(customerID);

            var userData = new WebUserData
            {
                CustomerID = customerID,
                CustomerName = userAccount.DisplayName ?? "",
                Email = (userAccount.Email ?? email).Trim().ToLowerInvariant(),
                Phone = customer?.Phone ?? "",
                Address = customer?.Address ?? "",
                Province = customer?.Province ?? ""
            };

            HttpContext.Session.SetObject("user", userData);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ==================== ĐĂNG XUẤT ====================

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("user");
            return RedirectToAction("Login");
        }

        // ==================== ĐĂNG KÝ ====================

        [HttpGet]
        public IActionResult Register()
        {
            if (CurrentUser != null)
                return RedirectToAction("Index", "Home");
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer data, string password, string confirmPassword)
        {
            data ??= new Customer();
            data.CustomerName = data.CustomerName?.Trim() ?? "";
            data.ContactName = data.ContactName?.Trim() ?? "";
            data.Email = data.Email?.Trim().ToLowerInvariant() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Province = data.Province?.Trim() ?? "";
            data.Address = data.Address?.Trim() ?? "";
            password = password?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            // Validate
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(data.CustomerName))
            {
                ModelState.AddModelError("CustomerName", "Họ và tên không được để trống!");
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(data.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống!");
                hasError = true;
            }
            else
            {
                try
                {
                    _ = new MailAddress(data.Email);
                }
                catch
                {
                    ModelState.AddModelError("Email", "Email không hợp lệ!");
                    hasError = true;
                }
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Mật khẩu không được để trống!");
                hasError = true;
            }
            else if (password.Length < 6)
            {
                ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự!");
                hasError = true;
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp!");
                hasError = true;
            }

            if (hasError) return View(data);

            // Kiểm tra email trùng
            try
            {
                var emailValid = await PartnerDataService.ValidateCustomerEmailAsync(data.Email);
                if (!emailValid)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng dùng email khác!");
                    return View(data);
                }
            }
            catch
            {
                ModelState.AddModelError("", "Không thể kiểm tra email do lỗi hệ thống. Vui lòng thử lại.");
                return View(data);
            }

            data.ContactName = string.IsNullOrWhiteSpace(data.ContactName)
                ? data.CustomerName
                : data.ContactName;
            data.IsLocked = false;

            // Thêm khách hàng với mật khẩu
            int newID = 0;
            try
            {
                newID = await PartnerDataService.AddCustomerWithPasswordAsync(data, password);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng dùng email khác!");
                return View(data);
            }
            catch
            {
                ModelState.AddModelError("", "Đăng ký thất bại do lỗi hệ thống hoặc cấu trúc CSDL chưa đồng bộ.");
                return View(data);
            }
            if (newID <= 0)
            {
                ModelState.AddModelError("", "Đăng ký thất bại, vui lòng thử lại!");
                return View(data);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (CurrentUser != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, string newPassword, string confirmPassword)
        {
            email = email?.Trim().ToLowerInvariant() ?? "";
            newPassword = newPassword?.Trim() ?? "";
            confirmPassword = confirmPassword?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("Email", "Vui lòng nhập email.");
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            if (newPassword != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
                return View();

            bool changed;
            try
            {
                changed = await UserAccountDataService.ChangePasswordAsync(email, newPassword);
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

        // ==================== HỒ SƠ CÁ NHÂN ====================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userData = CurrentUser;
            if (userData == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục";
                return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
            }

            var customer = await PartnerDataService.GetCustomerAsync(userData.CustomerID);
            if (customer == null) return RedirectToAction("Logout");

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer model)
        {
            var userData = CurrentUser;
            if (userData == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục";
                return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
            }

            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError("CustomerName", "Họ và tên không được để trống!");

            if (!ModelState.IsValid) return View(model);

            model.CustomerID = userData.CustomerID;
            model.IsLocked = false;

            bool ok;
            try
            {
                ok = await PartnerDataService.UpdateCustomerAsync(model);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể cập nhật thông tin do lỗi hệ thống. Vui lòng thử lại.");
                return View(model);
            }
            if (ok)
            {
                // Cập nhật lại session
                userData.CustomerName = model.CustomerName;
                userData.Phone = model.Phone ?? "";
                userData.Address = model.Address ?? "";
                userData.Province = model.Province ?? "";
                HttpContext.Session.SetObject("user", userData);

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Cập nhật thất bại, vui lòng thử lại!");
            return View(model);
        }

        // ==================== ĐỔI MẬT KHẨU ====================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (CurrentUser == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục";
                return RedirectToAction("Login", new { returnUrl = "/Account/ChangePassword" });
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userData = CurrentUser;
            if (userData == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục";
                return RedirectToAction("Login", new { returnUrl = "/Account/ChangePassword" });
            }

            bool hasError = false;
            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                ModelState.AddModelError("OldPassword", "Vui lòng nhập mật khẩu cũ!");
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải có ít nhất 6 ký tự!");
                hasError = true;
            }
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp!");
                hasError = true;
            }
            if (hasError) return View();

            // Kiểm tra mật khẩu cũ
            object? check;
            try
            {
                check = await UserAccountDataService.AuthorizeAsync(userData.Email, oldPassword);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể kiểm tra mật khẩu do lỗi hệ thống. Vui lòng thử lại.");
                return View();
            }
            if (check == null)
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác!");
                return View();
            }

            bool result;
            try
            {
                result = await UserAccountDataService.ChangePasswordAsync(userData.Email, newPassword);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể đổi mật khẩu do lỗi hệ thống. Vui lòng thử lại.");
                return View();
            }
            if (result)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Đổi mật khẩu thất bại, vui lòng thử lại!");
            return View();
        }
    }
}
