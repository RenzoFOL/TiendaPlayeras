using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using System.Text.RegularExpressions;

namespace TiendaPlayeras.Web.Controllers
{
    // Solo Employee y Admin pueden entrar
    [Authorize(Roles = "Employee,Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ProductsController(ApplicationDbContext db) => _db = db;

        // GET /Products
        [HttpGet("/Products")]
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View("Index", products); // Views/Products/Index.cshtml
        }

        // GET /Products/Create
        [HttpGet("/Products/Create")]
        public IActionResult Create()
            => View("Create", new Product { IsActive = true, IsCustomizable = false });

        // POST /Products/Create
        [HttpPost("/Products/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "El nombre es obligatorio.");
            if (model.BasePrice < 0)
                ModelState.AddModelError(nameof(model.BasePrice), "El precio base no puede ser negativo.");

            if (!ModelState.IsValid) return View("Create", model);

            model.Slug = Slugify(model.Name);
            model.CreatedAt = DateTime.UtcNow;

            _db.Products.Add(model);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Producto creado correctamente.";
            return Redirect("/Products");
        }

        // GET /Products/Edit/5
        [HttpGet("/Products/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View("Edit", product); // Views/Products/Edit.cshtml
        }

        // POST /Products/Edit/5
        [HttpPost("/Products/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            if (!ModelState.IsValid) return View("Edit", model);

            product.Name = model.Name;
            product.Description = model.Description;
            product.BasePrice = model.BasePrice;
            product.IsCustomizable = model.IsCustomizable;
            product.IsActive = model.IsActive;
            product.Slug = Slugify(model.Name);
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Producto actualizado.";
            return Redirect("/Products");
        }

        // POST /Products/ToggleActive/5
        [HttpPost("/Products/ToggleActive/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Ok"] = product.IsActive ? "Producto habilitado." : "Producto inhabilitado.";
            return Redirect("/Products");
        }

        [NonAction]
        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var slug = input.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug;
        }
    }
}