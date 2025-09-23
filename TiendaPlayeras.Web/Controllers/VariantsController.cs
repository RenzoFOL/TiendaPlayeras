using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Controllers
{
/// <summary>Gestión de Variantes por Producto.</summary>
[Authorize(Roles = "Admin,Employee")]
public class VariantsController : Controller
{
private readonly ApplicationDbContext _db;
public VariantsController(ApplicationDbContext db) => _db = db;


public async Task<IActionResult> Index(int productId)
{
ViewBag.ProductId = productId;
var list = await _db.ProductVariants.Include(v => v.Product)
.Where(v => v.ProductId == productId).ToListAsync();
return View(list);
}


public IActionResult Create(int productId) => View(new ProductVariant { ProductId = productId });


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductVariant model)
{
if (!ModelState.IsValid) return View(model);
_db.ProductVariants.Add(model);
await _db.SaveChangesAsync();
return RedirectToAction(nameof(Index), new { productId = model.ProductId });
}


public async Task<IActionResult> Edit(int id)
{
var v = await _db.ProductVariants.FindAsync(id);
if (v == null) return NotFound();
return View(v);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(ProductVariant model)
{
if (!ModelState.IsValid) return View(model);
_db.ProductVariants.Update(model);
await _db.SaveChangesAsync();
return RedirectToAction(nameof(Index), new { productId = model.ProductId });
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Toggle(int id)
{
var v = await _db.ProductVariants.FindAsync(id);
if (v == null) return NotFound();
v.IsActive = !v.IsActive;
await _db.SaveChangesAsync();
return RedirectToAction(nameof(Index), new { productId = v.ProductId });
}
}
}