using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Services;


namespace TiendaPlayeras.Web.Controllers
{
/// <summary>Carrito para invitado y cliente.</summary>
public class CartController : Controller
{
private readonly ApplicationDbContext _db;
private readonly ICartService _cart;
public CartController(ApplicationDbContext db, ICartService cart) { _db = db; _cart = cart; }


private string GetSessionId() => HttpContext.Session.Id; // usa SessionId actual
private string? GetUserId() => User.Identity?.IsAuthenticated == true ? User.GetUserId() : null; // método de extensión util


/// <summary>Vista del carrito.</summary>
public async Task<IActionResult> Index()
{
var cartId = await _cart.GetOrCreateCartIdAsync(GetUserId(), GetSessionId());
var items = await _db.CartItems.Include(i => i.ProductVariant).ThenInclude(v => v.Product)
.Where(i => i.CartId == cartId && i.IsActive).ToListAsync();
var sum = await _cart.GetSummaryAsync(cartId);
ViewBag.Subtotal = sum.subtotal; ViewBag.TotalItems = sum.totalItems;
return View(items);
}


/// <summary>Agregar ítem al carrito.</summary>
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Add(int variantId, int qty = 1)
{
var v = await _db.ProductVariants.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == variantId && x.IsActive);
if (v == null) return NotFound();
var cartId = await _cart.GetOrCreateCartIdAsync(GetUserId(), GetSessionId());
await _cart.AddAsync(cartId, variantId, Math.Max(1, qty), v.Price);
return RedirectToAction("Index");
}


/// <summary>Eliminar ítem (inhabilita).</summary>
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Remove(int id)
{
await _cart.RemoveAsync(id);
return RedirectToAction("Index");
}
}
}