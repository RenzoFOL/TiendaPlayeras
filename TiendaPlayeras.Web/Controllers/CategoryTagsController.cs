using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slugify;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using Npgsql;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryTagsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly SlugHelper _slug = new();
        private readonly ILogger<CategoryTagsController> _logger;

        public CategoryTagsController(ApplicationDbContext db, ILogger<CategoryTagsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var cats = await _db.Categories
                    .Include(c => c.Tags.Where(t => t.IsActive))
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(cats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categorías en Index");
                TempData["Error"] = "Error al cargar las categorías.";
                return View(new List<Category>());
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, string? description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "El nombre de la categoría es requerido.";
                    return RedirectToAction(nameof(Tags));
                }

                name = name.Trim();
                var slug = _slug.GenerateSlug(name);

                var existingCategory = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() || c.Slug == slug);

                if (existingCategory != null)
                {
                    TempData["Error"] = $"Ya existe una categoría con el nombre '{name}'.";
                    return RedirectToAction(nameof(Tags));
                }

                var cat = new Category 
                { 
                    Name = name, 
                    Slug = slug, 
                    Description = description?.Trim(), 
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Categories.Add(cat);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Categoría '{name}' creada correctamente.";
                return RedirectToAction(nameof(Tags));
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                TempData["Error"] = "Ya existe una categoría con ese nombre o slug.";
                _logger.LogWarning(ex, "Intento de crear categoría duplicada: {Name}", name);
                return RedirectToAction(nameof(Tags));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error inesperado al crear la categoría.";
                _logger.LogError(ex, "Error inesperado al crear categoría: {Name}", name);
                return RedirectToAction(nameof(Tags));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, string? description)
        {
            try
            {
                var c = await _db.Categories.FindAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Categoría no encontrada.";
                    return RedirectToAction(nameof(Tags));
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "El nombre de la categoría es requerido.";
                    return RedirectToAction(nameof(Tags));
                }

                name = name.Trim();
                var slug = _slug.GenerateSlug(name);

                var existingCategory = await _db.Categories
                    .FirstOrDefaultAsync(cat => 
                        cat.Id != id && 
                        (cat.Name.ToLower() == name.ToLower() || cat.Slug == slug));

                if (existingCategory != null)
                {
                    TempData["Error"] = $"Ya existe otra categoría con el nombre '{name}'.";
                    return RedirectToAction(nameof(Tags));
                }

                c.Name = name;
                c.Slug = slug;
                c.Description = description?.Trim();
                c.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Categoría '{name}' actualizada correctamente.";
                return RedirectToAction(nameof(Tags));
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                TempData["Error"] = "Ya existe otra categoría con ese nombre o slug.";
                _logger.LogWarning(ex, "Intento de editar categoría a nombre duplicado ID: {Id}", id);
                return RedirectToAction(nameof(Tags));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error inesperado al actualizar la categoría.";
                _logger.LogError(ex, "Error inesperado al editar categoría ID: {Id}", id);
                return RedirectToAction(nameof(Tags));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategory(int id)
        {
            try
            {
                var c = await _db.Categories.FindAsync(id);
                if (c == null)
                {
                    TempData["Error"] = "Categoría no encontrada.";
                    return RedirectToAction(nameof(Tags));
                }

                c.IsActive = !c.IsActive;
                c.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var action = c.IsActive ? "activada" : "desactivada";
                TempData["Success"] = $"Categoría '{c.Name}' {action} correctamente.";
                return RedirectToAction(nameof(Tags));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cambiar el estado de la categoría.";
                _logger.LogError(ex, "Error al toggle categoría ID: {Id}", id);
                return RedirectToAction(nameof(Tags));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag(int categoryId, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "El nombre de la etiqueta es requerido.";
                    return RedirectToAction(nameof(Tags));
                }

                name = name.Trim();
                var slug = _slug.GenerateSlug(name);

                var existingTag = await _db.Tags
                    .FirstOrDefaultAsync(t => 
                        t.CategoryId == categoryId && 
                        (t.Name.ToLower() == name.ToLower() || t.Slug == slug));

                if (existingTag != null)
                {
                    TempData["Error"] = $"Ya existe una etiqueta con el nombre '{name}' en esta categoría.";
                    return RedirectToAction(nameof(Tags));
                }

                var t = new Tag 
                { 
                    CategoryId = categoryId, 
                    Name = name, 
                    Slug = slug, 
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tags.Add(t);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Etiqueta '{name}' creada correctamente.";
                return RedirectToAction(nameof(Tags));
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                TempData["Error"] = "Ya existe una etiqueta con ese nombre o slug en esta categoría.";
                _logger.LogWarning(ex, "Intento de crear etiqueta duplicada: {Name} en categoría {CategoryId}", name, categoryId);
                return RedirectToAction(nameof(Tags));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error inesperado al crear la etiqueta.";
                _logger.LogError(ex, "Error inesperado al crear etiqueta: {Name}", name);
                return RedirectToAction(nameof(Tags));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTag(int id, string name, int categoryId)
        {
            try
            {
                var t = await _db.Tags.FindAsync(id);
                if (t == null)
                {
                    TempData["Error"] = "Etiqueta no encontrada.";
                    return RedirectToAction(nameof(Tags));
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "El nombre de la etiqueta es requerido.";
                    return RedirectToAction(nameof(Tags));
                }

                name = name.Trim();
                var slug = _slug.GenerateSlug(name);

                var existingTag = await _db.Tags
                    .FirstOrDefaultAsync(tag => 
                        tag.Id != id && 
                        tag.CategoryId == categoryId && 
                        (tag.Name.ToLower() == name.ToLower() || tag.Slug == slug));

                if (existingTag != null)
                {
                    TempData["Error"] = $"Ya existe una etiqueta con el nombre '{name}' en esta categoría.";
                    return RedirectToAction(nameof(Tags));
                }

                t.Name = name;
                t.Slug = slug;
                t.CategoryId = categoryId;
                t.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Etiqueta '{name}' actualizada correctamente.";
                return RedirectToAction(nameof(Tags));
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                TempData["Error"] = "Ya existe una etiqueta con ese nombre o slug en esta categoría.";
                _logger.LogWarning(ex, "Intento de editar etiqueta a nombre duplicado ID: {Id}", id);
                return RedirectToAction(nameof(Tags));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error inesperado al actualizar la etiqueta.";
                _logger.LogError(ex, "Error inesperado al editar etiqueta ID: {Id}", id);
                return RedirectToAction(nameof(Tags));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTag(int id)
        {
            try
            {
                var t = await _db.Tags.FindAsync(id);
                if (t == null)
                {
                    TempData["Error"] = "Etiqueta no encontrada.";
                    return RedirectToAction(nameof(Tags));
                }

                t.IsActive = !t.IsActive;
                t.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var action = t.IsActive ? "activada" : "desactivada";
                TempData["Success"] = $"Etiqueta '{t.Name}' {action} correctamente.";
                return RedirectToAction(nameof(Tags));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cambiar el estado de la etiqueta.";
                _logger.LogError(ex, "Error al toggle etiqueta ID: {Id}", id);
                return RedirectToAction(nameof(Tags));
            }
        }

        [HttpGet]
        public async Task<IActionResult> FindTags(string? q, int take = 20)
        {
            try
            {
                q ??= "";
                var list = await _db.Tags
                    .Include(t => t.Category)
                    .Where(t => t.IsActive && (t.Name.Contains(q) || t.Slug.Contains(q)))
                    .OrderBy(t => t.Name)
                    .Take(Math.Clamp(take, 5, 50))
                    .Select(t => new { t.Id, t.Name, Category = t.Category!.Name })
                    .ToListAsync();

                return Json(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en FindTags: {Query}", q);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Tags(int? categoryId, string? q, string? categorySearch, int catPage = 1, int tagPage = 1)
        {
            try
            {
                var cats = await _db.Categories
                    .Include(c => c.Tags)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var tagsQ = _db.Tags.Include(t => t.Category).AsQueryable();
                
                if (categoryId.HasValue) 
                    tagsQ = tagsQ.Where(t => t.CategoryId == categoryId.Value);
                
                if (!string.IsNullOrWhiteSpace(q))
                {
                    q = q.Trim();
                    tagsQ = tagsQ.Where(t => t.Name.Contains(q) || t.Slug.Contains(q));
                }

                var tags = await tagsQ
                    .OrderBy(t => t.Category!.Name)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                ViewBag.Categories = cats;
                ViewBag.CategoryId = categoryId;
                ViewBag.Q = q ?? "";
                ViewBag.CategorySearch = categorySearch ?? "";
                ViewBag.CategoryPage = catPage;
                ViewBag.TagPage = tagPage;

                return View(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista Tags");
                TempData["Error"] = "Error al cargar las categorías y etiquetas.";
                return View(new List<Tag>());
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTagInline(int categoryId, string name)
        {
            try
            {
                if (categoryId <= 0 || string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "Categoría y nombre de etiqueta son requeridos.";
                    return RedirectToAction(nameof(Tags));
                }

                name = name.Trim();
                var slug = _slug.GenerateSlug(name);

                var existingTag = await _db.Tags
                    .FirstOrDefaultAsync(t => 
                        t.CategoryId == categoryId && 
                        (t.Name.ToLower() == name.ToLower() || t.Slug == slug));

                if (existingTag != null)
                {
                    TempData["Error"] = $"Ya existe una etiqueta con el nombre '{name}' en esta categoría.";
                    return RedirectToAction(nameof(Tags), new { categoryId });
                }

                var t = new Tag 
                {
                    CategoryId = categoryId,
                    Name = name,
                    Slug = slug,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tags.Add(t);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Etiqueta '{name}' creada correctamente.";
                return RedirectToAction(nameof(Tags), new { categoryId });
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                TempData["Error"] = "Ya existe una etiqueta con ese nombre o slug en esta categoría.";
                _logger.LogWarning(ex, "Intento de crear etiqueta inline duplicada: {Name}", name);
                return RedirectToAction(nameof(Tags), new { categoryId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error inesperado al crear la etiqueta.";
                _logger.LogError(ex, "Error inesperado al crear etiqueta inline: {Name}", name);
                return RedirectToAction(nameof(Tags), new { categoryId });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTag(int id, int categoryId, string name, bool isActive = true)
        {
            try
            {
                var t = await _db.Tags.FindAsync(id);
                if (t == null)
                {
                    TempData["Error"] = "Etiqueta no encontrada.";
                    return RedirectToAction(nameof(Tags), new { categoryId });
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "El nombre de la etiqueta es requerido.";
                    return RedirectToAction(nameof(Tags), new { categoryId });
                }

                name = name.Trim();
                var slug = _slug.GenerateSlug(name);

                var existingTag = await _db.Tags
                    .FirstOrDefaultAsync(tag => 
                        tag.Id != id && 
                        tag.CategoryId == categoryId && 
                        (tag.Name.ToLower() == name.ToLower() || tag.Slug == slug));

                if (existingTag != null)
                {
                    TempData["Error"] = $"Ya existe una etiqueta con el nombre '{name}' en esta categoría.";
                    return RedirectToAction(nameof(Tags), new { categoryId });
                }

                t.CategoryId = categoryId;
                t.Name = name;
                t.Slug = slug;
                t.IsActive = isActive;
                t.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Etiqueta '{name}' actualizada correctamente.";
                return RedirectToAction(nameof(Tags), new { categoryId });
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                TempData["Error"] = "Ya existe una etiqueta con ese nombre o slug en esta categoría.";
                _logger.LogWarning(ex, "Intento de actualizar etiqueta a nombre duplicado ID: {Id}", id);
                return RedirectToAction(nameof(Tags), new { categoryId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error inesperado al actualizar la etiqueta.";
                _logger.LogError(ex, "Error inesperado al actualizar etiqueta ID: {Id}", id);
                return RedirectToAction(nameof(Tags), new { categoryId });
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException postgresEx)
            {
                return postgresEx.SqlState == "23505";
            }
            
            var errorMessage = ex.InnerException?.Message ?? "";
            return errorMessage.Contains("23505") || 
                   errorMessage.Contains("unique constraint") ||
                   errorMessage.Contains("duplicate key value") ||
                   errorMessage.ToLower().Contains("unique");
        }
    }
}