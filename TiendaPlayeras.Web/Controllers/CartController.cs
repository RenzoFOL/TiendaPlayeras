using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Services;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ICartService cart, UserManager<ApplicationUser> userManager)
        {
            _cart = cart;
            _userManager = userManager;
        }

        private string? CurrentUserId => _userManager.GetUserId(User);

        // -------- /Cart/Add - UNIFICADO (maneja tanto JSON como Form) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromBody] AddRequest? jsonRequest)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Json(new { ok = false, message = "No autorizado" });
            }

            try
            {
                int productId;
                string size;
                int qty;

                // Detectar si viene JSON o Form
                if (jsonRequest != null && jsonRequest.ProductId > 0)
                {
                    // Petición JSON
                    productId = jsonRequest.ProductId;
                    size = jsonRequest.Size ?? "M";
                    qty = jsonRequest.Qty;
                }
                else
                {
                    // Petición Form (fallback)
                    productId = Request.Form.ContainsKey("productId") 
                        ? int.Parse(Request.Form["productId"].ToString()) 
                        : 0;
                    size = Request.Form["size"].ToString() ?? "M";
                    qty = Request.Form.ContainsKey("qty") 
                        ? int.Parse(Request.Form["qty"].ToString()) 
                        : 1;
                }

                if (productId <= 0)
                {
                    return Json(new { ok = false, message = "ID de producto inválido" });
                }

                await _cart.AddAsync(CurrentUserId, productId, size, qty);
                var summary = await _cart.GetSummaryAsync(CurrentUserId);

                return Json(new { ok = true, count = summary.TotalItems, total = summary.TotalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // Clase para peticiones JSON
        public class AddRequest
        {
            public int ProductId { get; set; }
            public string? Size { get; set; }
            public int Qty { get; set; } = 1;
        }

        // -------- /Cart/Count (para los badges) ----------
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                return Json(new { count = 0, total = 0m });

            var summary = await _cart.GetSummaryAsync(CurrentUserId);
            return Json(new { count = summary.TotalItems, total = summary.TotalAmount });
        }

        // -------- /Cart/Drawer (contenido del offcanvas) ----------
        [HttpGet]
        public async Task<IActionResult> Drawer()
        {
            CartSummary vm;
            if (string.IsNullOrEmpty(CurrentUserId))
                vm = new CartSummary();
            else
                vm = await _cart.GetSummaryAsync(CurrentUserId);

            return PartialView("_CartDrawer", vm);
        }

        // -------- /Cart/Remove (JSON) ----------
        public class RemoveRequest { public int CartItemId { get; set; } }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromBody] RemoveRequest req)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                return Json(new { ok = false, message = "No autorizado" });

            try
            {
                await _cart.RemoveAsync(CurrentUserId, req.CartItemId);
                var summary = await _cart.GetSummaryAsync(CurrentUserId);

                return Json(new { ok = true, count = summary.TotalItems, total = summary.TotalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // -------- /Cart/UpdateQty ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQty([FromForm] int cartItemId, [FromForm] int qty)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            try
            {
                await _cart.UpdateQtyAsync(CurrentUserId, cartItemId, qty);
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // -------- /Cart/Clear ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            try
            {
                await _cart.ClearAsync(CurrentUserId);
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // -------- /Cart/Summary (para badges y totales) ----------
        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                return Json(new { count = 0, total = 0m, totalFormatted = "$0.00", hasItems = false });

            var summary = await _cart.GetSummaryAsync(CurrentUserId);
            var formatted = summary.TotalAmount.ToString("C", new System.Globalization.CultureInfo("es-MX"));

            return Json(new
            {
                count = summary.TotalItems,
                total = summary.TotalAmount,
                totalFormatted = formatted,
                hasItems = summary.TotalItems > 0
            });
        }

        // -------- /Cart/Index (vista completa del carrito) ----------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            CartSummary vm;
            if (string.IsNullOrEmpty(CurrentUserId))
                vm = new CartSummary();
            else
                vm = await _cart.GetSummaryAsync(CurrentUserId);

            return View(vm);
        }
    }
}