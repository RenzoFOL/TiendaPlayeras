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

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, string size, int quantity = 1)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = GetSessionId();

            await _cartService.AddToCartAsync(productId, size, quantity, userId, sessionId);
            
            TempData["Success"] = "Producto agregado al carrito";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index");
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
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        try
        {
            await _cartService.UpdateQuantityAsync(cartItemId, quantity);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        try
        {
            await _cartService.RemoveAsync(cartItemId);
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
}