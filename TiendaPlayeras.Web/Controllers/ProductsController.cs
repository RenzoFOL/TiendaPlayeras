using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Controllers
{
/// <summary>
/// CRUD de productos y variantes. Acceso restringido a Admin/Empleado.
/// </summary>
[Authorize(Roles = "Admin,Employee")]
public class ProductsController : Controller
{
private readonly ApplicationDbContext _db;
public ProductsController(ApplicationDbContext db) => _db = db;


/// <summary>Lista de productos activos.</summary>
public async Task<IActionResult> Index()
{
var productos = await _db.Products.Include(p => p.Variants).ToListAsync();
return View(productos);
}


/// <summary>Formulario de creación de producto.</summary>
public IActionResult Create() => View();


/// <summary>Persistencia de producto nuevo.</summary>
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Product model)
{
if (!ModelState.IsValid) return View(model);
_db.Add(model);
await _db.SaveChangesAsync();
return RedirectToAction(nameof(Index));
}


/// <summary>Edición de producto existente.</summary>
public async Task<IActionResult> Edit(int id)
{
var p = await _db.Products.FindAsync(id);
if (p == null) return NotFound();
return View(p);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Product model)
{
if (id != model.Id) return BadRequest();
if (!ModelState.IsValid) return View(model);
_db.Update(model);
await _db.SaveChangesAsync();
return RedirectToAction(nameof(Index));
}


/// <summary>Inhabilitar (no eliminar) un producto.</summary>
[HttpPost]
public async Task<IActionResult> Disable(int id)
{
var p = await _db.Products.FindAsync(id);
if (p == null) return NotFound();
p.IsActive = false;
await _db.SaveChangesAsync();
return RedirectToAction(nameof(Index));
}
}
}