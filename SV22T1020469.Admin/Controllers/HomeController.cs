using System.Diagnostics;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020469.BusinessLayers;
using SV22T1020605.Admin.Models;

namespace SV22T1020605.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý các trang chung của hệ thống quản trị (Trang chủ, Thông tin, Báo lỗi...)
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Hàm khởi tạo HomeController
        /// </summary>
        /// <param name="logger">Được inject tự động bởi hệ thống DI (Dependency Injection) của ASP.NET Core dùng để ghi log</param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Giao diện Trang chủ (Dashboard) của hệ thống.
        /// Thường dùng để hiển thị các biểu đồ, số liệu thống kê tổng quan (Tổng doanh thu, số đơn hàng mới,...).
        /// </summary>
        /// <returns>View Index.cshtml</returns>
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Bảng điều khiển (Dashboard)";
            var model = await ReportDataService.GetDashboardAsync(DateTime.Now.Year);
            return View(model);
        }

        /// <summary>
        /// Giao diện hiển thị các chính sách bảo mật hoặc thông tin quy định của hệ thống quản trị.
        /// </summary>
        /// <returns>View Privacy.cshtml</returns>
        public IActionResult Privacy()
        {
            ViewBag.Title = "Chính sách & Quy định";
            return View();
        }

        /// <summary>
        /// Giao diện báo lỗi chung của toàn hệ thống.
        /// Action này sẽ tự động được gọi đến khi ứng dụng gặp Exception nhờ cấu hình UseExceptionHandler trong Program.cs.
        /// Thuộc tính ResponseCache ngăn trình duyệt lưu cache trang này để luôn hiển thị mã lỗi mới nhất.
        /// </summary>
        /// <returns>View Error.cshtml kèm theo model ErrorViewModel chứa RequestId để debug</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Trả về View báo lỗi, gắn kèm RequestId (lấy từ Activity hiện tại hoặc TraceIdentifier của HTTP Context)
            // Giúp lập trình viên dễ dàng tra cứu log theo ID khi có lỗi xảy ra.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}