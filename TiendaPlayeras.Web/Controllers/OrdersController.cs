using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using Slugify;
using TiendaPlayeras.Web.Services;
using System.Security.Claims;

namespace TiendaPlayeras.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrdersController(IOrderService orderService, ICartService cartService, IHttpContextAccessor httpContextAccessor)
    {
        _orderService = orderService;
        _cartService = cartService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(int? addressId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = GetSessionId();

            var cart = await _cartService.GetCartWithDetailsAsync(userId, sessionId);

            if (!cart.Items.Any())
            {
                TempData["Error"] = "El carrito está vacío";
                return RedirectToAction("Index", "Cart");
            }

            var order = await _orderService.CreateOrderAsync(userId, addressId, cart.Items.ToList());

            // Limpiar carrito después de crear la orden
            await _cartService.ClearAsync(cart.Id);

            TempData["Success"] = $"¡Orden creada exitosamente! Número de orden: {order.OrderNumber}";
            return RedirectToAction("Details", new { id = order.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear la orden: {ex.Message}";
            return RedirectToAction("Index", "Cart");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            TempData["Error"] = "Orden no encontrada";
            return RedirectToAction("Index");
        }

        // Verificar que el usuario es el dueño de la orden
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (order.UserId != userId && !User.IsInRole("Admin"))
        {
            TempData["Error"] = "No tienes permisos para ver esta orden";
            return RedirectToAction("Index");
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return View(orders);
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
}