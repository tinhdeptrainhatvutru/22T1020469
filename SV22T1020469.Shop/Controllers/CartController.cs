using Microsoft.AspNetCore.Mvc;
using SV22T1020469.BusinessLayers;
using SV22T1020469.Shop.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020469.Shop.Controllers
{
    public class CartController : Controller
    {
        private const string CART_KEY = "cart";

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>(CART_KEY)
                   ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CART_KEY, cart);
        }

        /// <summary>Đường dẫn quay lại sau khi đăng nhập (từ Referer, an toàn local).</summary>
        private string GetReturnUrlAfterCartLogin()
        {
            const string fallback = "/Product";
            var referer = Request.Headers.Referer.FirstOrDefault();
            if (string.IsNullOrEmpty(referer) || !Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                return fallback;
            var pathAndQuery = uri.PathAndQuery;
            if (string.IsNullOrEmpty(pathAndQuery) || pathAndQuery == "/")
                return fallback;
            if (pathAndQuery.StartsWith("/Cart/Add", StringComparison.OrdinalIgnoreCase))
                return fallback;
            if (pathAndQuery.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase))
                return fallback;
            if (!Url.IsLocalUrl(pathAndQuery))
                return fallback;
            return pathAndQuery;
        }

        // GET: /Cart — Xem giỏ hàng (bắt buộc đăng nhập)
        public IActionResult Index()
        {
            if (HttpContext.Session.GetObject<WebUserData>("user") == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart", reason = "viewcart" });
            return View(GetCart());
        }

        // GET: /Cart/Add?id=x&qty=1 — Thêm sản phẩm (AJAX JSON hoặc redirect với TempData)
        public async Task<IActionResult> Add(int id, int qty = 1, bool redirect = false)
        {
            if (qty <= 0) qty = 1;

            if (HttpContext.Session.GetObject<WebUserData>("user") == null)
            {
                var returnUrl = GetReturnUrlAfterCartLogin();
                var loginUrl = Url.Action("Login", "Account", new { returnUrl, reason = "cart" });
                if (string.IsNullOrEmpty(loginUrl))
                    loginUrl = "/Account/Login";
                if (redirect)
                    return Redirect(loginUrl);
                return Json(new { success = false, requireLogin = true, redirectUrl = loginUrl });
            }

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                if (redirect)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại!";
                    return RedirectToAction("Index", "Product");
                }
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductID == id);

            if (item == null)
            {
                if (qty > product.Quantity)
                {
                    string message = $"Sản phẩm này chỉ còn [{product.Quantity}] cái trong kho!";
                    if (redirect)
                    {
                        TempData["ErrorMessage"] = message;
                        return RedirectToAction("Index", "Product");
                    }
                    return Json(new { success = false, message });
                }

                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Photo = product.Photo ?? "",
                    SalePrice = product.Price,
                    Quantity = qty
                });
            }
            else
            {
                int newQty = item.Quantity + qty;
                if (newQty > product.Quantity)
                {
                    string message = $"Sản phẩm này chỉ còn [{product.Quantity}] cái trong kho!";
                    if (redirect)
                    {
                        TempData["ErrorMessage"] = message;
                        return RedirectToAction("Index", "Product");
                    }
                    return Json(new { success = false, message });
                }
                item.Quantity = newQty;
            }

            SaveCart(cart);

            if (redirect)
            {
                TempData["SuccessMessage"] = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng!";
                return RedirectToAction("Index");
            }

            return Json(new
            {
                success = true,
                message = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng!",
                count = cart.Sum(x => x.Quantity),
                total = cart.Sum(x => x.Total)
            });
        }

        // POST: /Cart/Update — Cập nhật số lượng
        [HttpPost]
        public async Task<IActionResult> Update(int id, string? qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductID == id);
            if (item == null)
            {
                SaveCart(cart);
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(qty)
                || !int.TryParse(qty.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                TempData["ErrorMessage"] = "Số lượng không hợp lệ. Vui lòng nhập số nguyên.";
                return RedirectToAction("Index");
            }

            if (parsed <= 0)
            {
                cart.Remove(item);
                TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            else
            {
                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không còn tồn tại.";
                    return RedirectToAction("Index");
                }
                if (parsed > product.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm này chỉ còn [{product.Quantity}] cái trong kho!";
                    return RedirectToAction("Index");
                }
                item.Quantity = parsed;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // GET: /Cart/Remove?id=x — Xóa sản phẩm
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductID == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            return RedirectToAction("Index");
        }

        // GET: /Cart/Clear — Xóa toàn bộ giỏ
        public IActionResult Clear()
        {
            SaveCart(new List<CartItem>());
            TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction("Index");
        }

        // GET: /Cart/Count — Số lượng items (cho badge navbar)
        public IActionResult Count()
        {
            int count = GetCart().Sum(x => x.Quantity);
            return Json(new { count });
        }
    }
}
