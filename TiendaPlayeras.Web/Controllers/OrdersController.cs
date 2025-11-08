using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TiendaPlayeras.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    // Checkout desactivado
    [HttpPost]
    public IActionResult Checkout(int? addressId)
    {
        TempData["Error"] = "El flujo de carrito/checkout está desactivado.";
        return RedirectToAction(nameof(Index));
    }

    // Detalles desactivados (evitamos depender de servicios/modelos)
    [HttpGet]
    public IActionResult Details(int id)
    {
        TempData["Error"] = "Los detalles de pedidos están desactivados.";
        return RedirectToAction(nameof(Index));
    }

    // Índice neutral
    [HttpGet]
    public IActionResult Index()
    {
        return View("Disabled"); // Vista sencilla informativa
    }
}
