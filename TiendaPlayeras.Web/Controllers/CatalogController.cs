using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>Catálogo público: listado y detalle de productos.</summary>
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CatalogController(ApplicationDbContext db) => _db = db;

        /// <summary>Lista productos activos con buscador básico.</summary>
        public async Task<IActionResult> Index(string? q, string? tag, string? category, int page = 1, int pageSize = 12)
        {
            var query = _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                        .ThenInclude(t => t!.Category)
                .Where(p => p.IsActive);

            // Búsqueda por texto
            if (!string.IsNullOrWhiteSpace(q)) 
            {
                q = q.Trim();
                query = query.Where(p => 
                    p.Name.Contains(q) || 
                    p.Description!.Contains(q) ||
                    p.Slug.Contains(q));
            }

            // Filtro por tag
            if (!string.IsNullOrWhiteSpace(tag))
            {
                tag = tag.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductTags.Any(pt =>
                        pt.IsActive &&
                        pt.Tag != null &&
                        pt.Tag.IsActive &&
                        (pt.Tag.Slug.ToLower() == tag || pt.Tag.Name.ToLower().Contains(tag))
                    ));
            }

            // Filtro por categoría
            if (!string.IsNullOrWhiteSpace(category))
            {
                category = category.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductTags.Any(pt =>
                        pt.IsActive &&
                        pt.Tag != null &&
                        pt.Tag.IsActive &&
                        pt.Tag.Category != null &&
                        pt.Tag.Category.IsActive &&
                        (pt.Tag.Category.Slug.ToLower() == category || pt.Tag.Category.Name.ToLower().Contains(category))
                    ));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Total = total; 
            ViewBag.Page = page; 
            ViewBag.PageSize = pageSize; 
            ViewBag.Q = q;
            ViewBag.Tag = tag;
            ViewBag.Category = category;

            return View(items);
        }

        /// <summary>Detalle por slug, incluye imágenes y tags.</summary>
        public async Task<IActionResult> Product(string slug)
        {
            // ✅ ACTUALIZADO: Incluir imágenes y tags (no variantes)
            var product = await _db.Products
                .Include(p => p.ProductImages.OrderBy(pi => pi.DisplayOrder))
                .Include(p => p.ProductTags.Where(pt => pt.IsActive))
                    .ThenInclude(pt => pt.Tag)
                        .ThenInclude(t => t!.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null) 
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToAction("Index");
            }

            // ✅ NUEVO: Productos relacionados (mismos tags)
            var relatedProducts = await _db.Products
                .Include(p => p.ProductImages)
                .Where(p => p.Id != product.Id && p.IsActive)
                .Where(p => p.ProductTags.Any(pt => 
                    pt.IsActive && 
                    product.ProductTags.Select(x => x.TagId).Contains(pt.TagId)))
                .OrderBy(p => p.Name)
                .Take(4)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // ✅ NUEVO: Búsqueda rápida para autocompletado
        [HttpGet]
        public async Task<IActionResult> QuickSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var products = await _db.Products
                .Where(p => p.IsActive && 
                    (p.Name.Contains(term) || p.Slug.Contains(term)))
                .Select(p => new 
                { 
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    price = p.BasePrice,
                    image = p.MainImagePath
                })
                .Take(10)
                .ToListAsync();

            return Json(products);
        }

        // ✅ NUEVO: Obtener productos por categoría
        [HttpGet]
        public async Task<IActionResult> ByCategory(string categorySlug)
        {
            var products = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                        .ThenInclude(t => t!.Category)
                .Where(p => p.IsActive &&
                    p.ProductTags.Any(pt =>
                        pt.IsActive &&
                        pt.Tag != null &&
                        pt.Tag.IsActive &&
                        pt.Tag.Category != null &&
                        pt.Tag.Category.IsActive &&
                        pt.Tag.Category.Slug == categorySlug))
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();

            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Slug == categorySlug && c.IsActive);

            ViewBag.CategoryName = category?.Name ?? "Categoría";
            ViewBag.CategorySlug = categorySlug;

            return View("Index", products);
        }

        // ✅ NUEVO: Obtener productos por tag
        [HttpGet]
        public async Task<IActionResult> ByTag(string tagSlug)
        {
            var products = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive &&
                    p.ProductTags.Any(pt =>
                        pt.IsActive &&
                        pt.Tag != null &&
                        pt.Tag.IsActive &&
                        pt.Tag.Slug == tagSlug))
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();

            var tag = await _db.Tags
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Slug == tagSlug && t.IsActive);

            ViewBag.TagName = tag?.Name ?? "Etiqueta";
            ViewBag.TagSlug = tagSlug;
            ViewBag.CategoryName = tag?.Category?.Name;

            return View("Index", products);
        }
    }
}