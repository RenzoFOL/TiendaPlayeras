using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Services;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>Carrito para invitado y cliente.</summary>
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(ICartService cartService, IHttpContextAccessor httpContextAccessor)
        {
            _cartService = cartService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetSessionId()
        {
            var sessionId = _httpContextAccessor.HttpContext?.Session.GetString("CartSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                _httpContextAccessor.HttpContext?.Session.SetString("CartSessionId", sessionId);
            }
            return sessionId;
        }

        // ✅ SOLO UN MÉTODO AddToCart - ELIMINA EL DUPLICADO
        // ✅ MANTÉN SOLO ESTE MÉTODO
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionId = GetSessionId();

                Console.WriteLine($"🛒 Agregando producto {request.ProductId}, talla: {request.Size}, cantidad: {request.Quantity}");

                await _cartService.AddToCartAsync(request.ProductId, request.Size, request.Quantity, userId, sessionId);
                
                return Json(new { success = true, message = "Producto agregado al carrito" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en AddToCart: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = GetSessionId();

            var cart = await _cartService.GetCartWithDetailsAsync(userId, sessionId);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                await _cartService.UpdateQuantityAsync(request.CartItemId, request.Quantity);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            try
            {
                await _cartService.RemoveAsync(request.CartItemId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionId = GetSessionId();
                
                var cartId = await _cartService.GetOrCreateCartIdAsync(userId, sessionId);
                await _cartService.ClearAsync(cartId);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionId = GetSessionId();
                
                var cartId = await _cartService.GetOrCreateCartIdAsync(userId, sessionId);
                var summary = await _cartService.GetSummaryAsync(cartId);
                
                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }

    // DTOs para las requests
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveItemRequest
    {
        public int CartItemId { get; set; }
    }
}