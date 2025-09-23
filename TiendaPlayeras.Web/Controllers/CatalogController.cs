using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;


namespace TiendaPlayeras.Web.Controllers
{
/// <summary>Catálogo público: listado y detalle de productos.</summary>
public class CatalogController : Controller
{
private readonly ApplicationDbContext _db;
public CatalogController(ApplicationDbContext db) => _db = db;


/// <summary>Lista productos activos con buscador básico.</summary>
public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 12)
{
var query = _db.Products.Where(p => p.IsActive);
if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));
var total = await query.CountAsync();
var items = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
ViewBag.Total = total; ViewBag.Page = page; ViewBag.PageSize = pageSize; ViewBag.Q = q;
return View(items);
}


/// <summary>Detalle por slug, incluye variantes activas.</summary>
public async Task<IActionResult> Product(string slug)
{
var p = await _db.Products.Include(p => p.Variants.Where(v => v.IsActive)).FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
if (p == null) return NotFound();
return View(p);
}
}
}