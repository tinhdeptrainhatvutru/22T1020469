using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020605.Admin.AppCodes;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Models.Common;
using SV22T1020469.Models.Partner;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020605.Admin.Controllers
{
    [Authorize] // BƯỚC 1: BẢO MẬT - Khóa Controller, bắt buộc đăng nhập
    public class ShipperController : Controller
    {
        private const string SHIPPER_SEARCH = "ShipperSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH) ?? new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 20,
                SearchValue = ""
            };
            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
            var result = await PartnerDataService.ListShippersAsync(input);

            // BƯỚC 2: FIX LỖI AJAX - Phải trả về PartialView để nhúng vào thẻ <div>
            return PartialView("Search", result);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var data = new Shipper() { ShipperID = 0 };
            return View("Edit", data);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật người giao hàng";
            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null) return RedirectToAction("Index");

            return View(shipper);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Shipper data)
        {
            // BƯỚC 3: CHUẨN HÓA DỮ LIỆU - Cắt khoảng trắng dư thừa
            data.ShipperName = data.ShipperName?.Trim() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";

            // VALIDATE
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Điện thoại không được để trống");
            else
            {
                int digits = data.Phone.Count(char.IsDigit);
                if (digits < 7 || digits > 20)
                    ModelState.AddModelError("Phone", "Số điện thoại phải có từ 7 đến 20 chữ số");
            }

            if (!string.IsNullOrWhiteSpace(data.Phone))
            {
                bool inUsePhone = await PartnerDataService.InUseShipperPhoneAsync(data.Phone, data.ShipperID);
                if (inUsePhone)
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng!");
            }

            // XỬ LÝ LỖI
            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật người giao hàng";
                return View("Edit", data);
            }

            // LƯU DB
            if (data.ShipperID == 0)
                await PartnerDataService.AddShipperAsync(data);
            else
                await PartnerDataService.UpdateShipperAsync(data);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa người giao hàng";
            var shipper = await PartnerDataService.GetShipperAsync(id);
            if (shipper == null) return RedirectToAction("Index");
            return View(shipper);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, bool confirm = true)
        {
            try
            {
                bool inUse = await PartnerDataService.IsUsedShipperAsync(id);
                if (inUse)
                {
                    TempData["ErrorMessage"] = "Không thể xóa người giao hàng này vì đã có dữ liệu giao hàng liên quan!";
                    return RedirectToAction("Index");
                }

                bool deleted = await PartnerDataService.DeleteShipperAsync(id);
                if (!deleted)
                {
                    TempData["ErrorMessage"] = "Không thể xóa vì dữ liệu đang được sử dụng ở nơi khác.";
                    return RedirectToAction("Index");
                }

                TempData["SuccessMessage"] = "Đã xóa người giao hàng thành công.";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể xóa vì dữ liệu đang được sử dụng ở nơi khác.";
                return RedirectToAction("Index");
            }
        }
    }
}