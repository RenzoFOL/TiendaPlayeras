using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using System.Text.RegularExpressions;
using Slugify;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext db, ILogger<ProductsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /Products
        [HttpGet("/Products")]
        public async Task<IActionResult> Index(string? q, string? tag, string sort = "az", int page = 1, int pageSize = 20)
        {
            try
            {
                // Query base con todas las relaciones necesarias
                var query = _db.Products
                    .Include(p => p.ProductTags)
                        .ThenInclude(pt => pt.Tag)
                            .ThenInclude(t => t!.Category)
                    .AsQueryable();

                // Filtro por búsqueda general
                if (!string.IsNullOrWhiteSpace(q))
                {
                    q = q.Trim();
                    query = query.Where(p =>
                        p.Name.Contains(q) ||
                        p.Description!.Contains(q) ||
                        p.Id.ToString().Contains(q) ||
                        p.Slug.Contains(q)
                    );
                }

                // Filtro por etiqueta
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tag = tag.Trim().ToLower();
                    query = query.Where(p =>
                        p.ProductTags.Any(pt =>
                            pt.IsActive &&
                            pt.Tag != null &&
                            pt.Tag.IsActive &&
                            (pt.Tag.Slug.ToLower() == tag || pt.Tag.Name.ToLower().Contains(tag))
                        )
                    );
                }

                // Ordenamiento
                query = sort?.ToLower() switch
                {
                    "za" => query.OrderByDescending(p => p.Name),
                    "price_asc" => query.OrderBy(p => p.BasePrice),
                    "price_desc" => query.OrderByDescending(p => p.BasePrice),
                    "id_asc" => query.OrderBy(p => p.Id),
                    "id_desc" => query.OrderByDescending(p => p.Id),
                    _ => query.OrderBy(p => p.Name) // "az" por defecto
                };

                // Conteo total
                var total = await query.CountAsync();

                // Paginación
                pageSize = Math.Clamp(pageSize, 10, 100);
                page = Math.Max(1, page);

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                // ViewBag para la vista
                ViewBag.Total = total;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Q = q ?? "";
                ViewBag.Tag = tag ?? "";
                ViewBag.Sort = sort ?? "az";

                _logger.LogInformation(
                    "Productos cargados: {Count} de {Total}. Filtros: q={Query}, tag={Tag}, sort={Sort}",
                    products.Count, total, q ?? "none", tag ?? "none", sort
                );

                return View("Index", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar lista de productos con filtros");
                TempData["Error"] = "Error al cargar los productos.";
                return View("Index", new List<Product>());
            }
        }

        // GET /Products/Create
        [HttpGet("/Products/Create")]
        public IActionResult Create()
        {
            return View("Create", new Product { IsActive = true, IsCustomizable = false });
        }

        // POST /Products/Create - VERSIÓN CORREGIDA
        [HttpPost("/Products/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? image)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(model.Name), "El nombre del producto es obligatorio.");
                }
                else
                {
                    // Verificar si ya existe un producto con el mismo nombre
                    var existingProduct = await _db.Products
                        .FirstOrDefaultAsync(p => p.Name.ToLower() == model.Name.Trim().ToLower());

                    if (existingProduct != null)
                    {
                        ModelState.AddModelError(nameof(model.Name), "Ya existe un producto con este nombre.");
                    }
                }

                if (model.BasePrice < 0)
                {
                    ModelState.AddModelError(nameof(model.BasePrice), "El precio base no puede ser negativo.");
                }

                // ⚠️ CAMBIO: Hacer imagen opcional o validarla según tu necesidad
                // Opción A: Hacerla opcional (RECOMENDADO)
                if (image != null)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
                    if (!allowedTypes.Contains(image.ContentType))
                    {
                        ModelState.AddModelError("Image", "Formato de imagen no válido. Use JPG, PNG, WebP o GIF.");
                    }

                    if (image.Length > 10 * 1024 * 1024) // 10MB
                    {
                        ModelState.AddModelError("Image", "La imagen es demasiado grande. Máximo 10MB permitidos.");
                    }
                }

                // Opción B: Hacerla obligatoria (si prefieres)
                /*
                if (image == null || image.Length == 0)
                {
                    ModelState.AddModelError("Image", "La imagen del producto es obligatoria.");
                }
                else
                {
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
                    if (!allowedTypes.Contains(image.ContentType))
                    {
                        ModelState.AddModelError("Image", "Formato de imagen no válido.");
                    }
                    if (image.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("Image", "La imagen es demasiado grande.");
                    }
                }
                */

                if (!ModelState.IsValid)
                {
                    return View("Create", model);
                }

                // Crear producto con slug único
                var slugger = new SlugHelper();
                var baseSlug = slugger.GenerateSlug(model.Name);
                model.Slug = await EnsureUniqueProductSlugAsync(baseSlug);
                model.CreatedAt = DateTime.UtcNow;
                model.IsActive = true;

                _db.Products.Add(model);
                await _db.SaveChangesAsync(); // Guardar para obtener el ID

                // Guardar imagen si se proporcionó
                if (image != null && image.Length > 0)
                {
                    try
                    {
                        model.MainImagePath = await SaveProductImageAsync(model.Id, image);
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al guardar imagen para producto ID: {ProductId}", model.Id);
                        // No fallar todo el proceso, solo advertir
                        TempData["Warning"] = "Producto creado pero hubo un error al guardar la imagen.";
                    }
                }

                TempData["Success"] = $"Producto '{model.Name}' creado correctamente.";
                return RedirectToAction("Edit", new { id = model.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear producto: {ProductName}", model.Name);
                ModelState.AddModelError("", "Error al guardar el producto en la base de datos.");
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear producto: {ProductName}", model.Name);
                ModelState.AddModelError("", "Error inesperado al crear el producto.");
                return View("Create", model);
            }
        }

        // ⚠️ NUEVO: Acción separada para guardar tags desde Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProductWithTags(int id, Product model, IFormFile? image, [FromForm] int[] tagIds)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var existing = await _db.Products.FindAsync(id);
                if (existing == null)
                {
                    TempData["Error"] = "Producto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Validaciones
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(model.Name), "El nombre es obligatorio.");
                    return View("Edit", model);
                }

                var duplicateProduct = await _db.Products
                    .FirstOrDefaultAsync(p => p.Id != id && p.Name.ToLower() == model.Name.Trim().ToLower());

                if (duplicateProduct != null)
                {
                    ModelState.AddModelError(nameof(model.Name), "Ya existe otro producto con este nombre.");
                    return View("Edit", model);
                }

                // Actualizar producto
                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.BasePrice = model.BasePrice;
                existing.IsCustomizable = model.IsCustomizable;
                existing.IsActive = model.IsActive;

                var slugger = new SlugHelper();
                var newSlug = slugger.GenerateSlug(model.Name);
                existing.Slug = await EnsureUniqueProductSlugAsync(newSlug, id);
                existing.UpdatedAt = DateTime.UtcNow;

                // Guardar imagen si se proporcionó
                if (image != null && image.Length > 0)
                {
                    existing.MainImagePath = await SaveProductImageAsync(existing.Id, image);
                }

                await _db.SaveChangesAsync();

                // Actualizar tags
                var validTagIds = tagIds?.Where(t => t > 0).Distinct().ToArray() ?? Array.Empty<int>();

                var currentActiveTags = await _db.ProductTags
                    .Where(pt => pt.ProductId == id && pt.IsActive)
                    .ToListAsync();

                var currentTagIds = currentActiveTags.Select(pt => pt.TagId).ToHashSet();
                var newTagIds = validTagIds.ToHashSet();

                // Desactivar tags removidos
                foreach (var pt in currentActiveTags.Where(pt => !newTagIds.Contains(pt.TagId)))
                {
                    pt.IsActive = false;
                }

                // Activar/crear nuevos tags
                foreach (var tagId in newTagIds.Where(tid => !currentTagIds.Contains(tid)))
                {
                    var existingRelation = await _db.ProductTags
                        .FirstOrDefaultAsync(pt => pt.ProductId == id && pt.TagId == tagId);

                    if (existingRelation != null)
                    {
                        existingRelation.IsActive = true;
                    }
                    else
                    {
                        _db.ProductTags.Add(new ProductTag
                        {
                            ProductId = id,
                            TagId = tagId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Producto '{existing.Name}' actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al guardar producto con tags ID: {ProductId}", id);
                ModelState.AddModelError("", "Error al guardar los cambios.");
                return View("Edit", model);
            }
        }

        // GET /Products/Edit/5
        [HttpGet("/Products/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    TempData["Error"] = "Producto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }
                return View("Edit", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar producto para editar ID: {ProductId}", id);
                TempData["Error"] = "Error al cargar el producto.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST /Products/Edit/5 - CON IMAGEN
        [HttpPost("/Products/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? image)
        {
            try
            {
                var existing = await _db.Products.FindAsync(id);
                if (existing == null)
                {
                    TempData["Error"] = "Producto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                    return View("Edit", model);

                // Verificar si ya existe otro producto con el mismo nombre
                var duplicateProduct = await _db.Products
                    .FirstOrDefaultAsync(p => p.Id != id && p.Name.ToLower() == model.Name.Trim().ToLower());

                if (duplicateProduct != null)
                {
                    ModelState.AddModelError(nameof(model.Name), "Ya existe otro producto con este nombre.");
                    return View("Edit", model);
                }

                // Validar imagen si se proporcionó
                if (image != null)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
                    if (!allowedTypes.Contains(image.ContentType))
                    {
                        ModelState.AddModelError("Image", "Formato de imagen no válido. Use JPG, PNG, WebP o GIF.");
                        return View("Edit", model);
                    }

                    if (image.Length > 10 * 1024 * 1024) // 10MB
                    {
                        ModelState.AddModelError("Image", "La imagen es demasiado grande. Máximo 10MB permitidos.");
                        return View("Edit", model);
                    }
                }

                // Actualizar propiedades con slug robusto y único
                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.BasePrice = model.BasePrice;
                existing.IsCustomizable = model.IsCustomizable;
                existing.IsActive = model.IsActive;

                var slugger = new SlugHelper();
                var newSlug = slugger.GenerateSlug(model.Name);
                existing.Slug = await EnsureUniqueProductSlugAsync(newSlug, id);

                existing.UpdatedAt = DateTime.UtcNow;

                // Guardar imagen si se proporcionó
                if (image != null)
                {
                    try
                    {
                        existing.MainImagePath = await SaveProductImageAsync(existing.Id, image);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Image", $"Error al guardar la imagen: {ex.Message}");
                        return View("Edit", model);
                    }
                }

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Producto '{existing.Name}' actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al editar producto ID: {ProductId}", id);
                ModelState.AddModelError("", "Error al actualizar el producto en la base de datos.");
                return View("Edit", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar producto ID: {ProductId}", id);
                ModelState.AddModelError("", "Error inesperado al actualizar el producto.");
                return View("Edit", model);
            }
        }

        // POST /Products/ToggleActive/5
        [HttpPost("/Products/ToggleActive/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    TempData["Error"] = "Producto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var action = product.IsActive ? "habilitado" : "inhabilitado";
                TempData["Success"] = $"Producto '{product.Name}' {action} correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del producto ID: {ProductId}", id);
                TempData["Error"] = "Error al cambiar el estado del producto.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: devuelve tags del producto
        [HttpGet]
        public async Task<IActionResult> GetTags(int id)
        {
            try
            {
                _logger.LogInformation("GetTags llamado para producto ID: {ProductId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("ID de producto inválido: {ProductId}", id);
                    return Json(new { error = "ID inválido" });
                }

                // Verificar que el producto existe
                var productExists = await _db.Products.AnyAsync(p => p.Id == id);
                if (!productExists)
                {
                    _logger.LogWarning("Producto no encontrado ID: {ProductId}", id);
                    return Json(new { error = "Producto no encontrado" });
                }

                var tags = await _db.ProductTags
                    .Include(pt => pt.Tag)
                        .ThenInclude(t => t!.Category)
                    .Where(pt =>
                        pt.ProductId == id &&
                        pt.IsActive &&
                        pt.Tag != null &&
                        pt.Tag.IsActive
                    )
                    .OrderBy(pt => pt.Tag!.Category!.Name)
                    .ThenBy(pt => pt.Tag!.Name)
                    .Select(pt => new
                    {
                        tagId = pt.TagId,
                        name = pt.Tag!.Name,
                        category = pt.Tag!.Category!.Name,
                        slug = pt.Tag!.Slug
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    "GetTags completado para producto ID: {ProductId}. Tags encontrados: {TagCount}",
                    id, tags.Count
                );

                return Json(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tags del producto ID: {ProductId}", id);
                return Json(new { error = "Error al cargar las etiquetas" });
            }
        }

        // POST: reemplaza tags del producto con una lista de TagId[]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTags(int id, [FromForm] int[] tagIds)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Iniciando guardado de tags para producto ID: {ProductId}, tags recibidos: {TagCount}", id, tagIds?.Length ?? 0);

                // Validar ID del producto
                if (id <= 0)
                {
                    _logger.LogWarning("ID de producto inválido: {ProductId}", id);
                    return Json(new { error = "ID de producto inválido" });
                }

                // Verificar existencia del producto
                var product = await _db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    _logger.LogWarning("Producto no encontrado ID: {ProductId}", id);
                    return Json(new { error = "Producto no encontrado" });
                }

                // Validar y limpiar tagIds
                var validTagIds = tagIds?
                    .Where(tagId => tagId > 0)
                    .Distinct()
                    .ToArray() ?? Array.Empty<int>();

                _logger.LogDebug("Tags válidos a procesar: {ValidTagCount}", validTagIds.Length);

                // Verificar existencia de los tags
                if (validTagIds.Any())
                {
                    var existingTags = await _db.Tags
                        .Where(t => validTagIds.Contains(t.Id) && t.IsActive)
                        .Select(t => t.Id)
                        .ToListAsync();

                    var nonExistentTags = validTagIds.Except(existingTags).ToList();

                    if (nonExistentTags.Any())
                    {
                        _logger.LogWarning("Algunos tags no existen o están inactivos: {NonExistentTags}", string.Join(", ", nonExistentTags));
                        // Continuamos solo con los tags existentes
                        validTagIds = existingTags.ToArray();
                    }
                }

                // Obtener relaciones actuales activas
                var currentActiveTags = await _db.ProductTags
                    .Where(pt => pt.ProductId == id && pt.IsActive)
                    .ToListAsync();

                _logger.LogDebug("Relaciones activas actuales: {CurrentCount}", currentActiveTags.Count);

                // Crear sets para comparación eficiente
                var currentTagIds = currentActiveTags.Select(pt => pt.TagId).ToHashSet();
                var newTagIds = validTagIds.ToHashSet();

                // Tags a desactivar (están actualmente activos pero no en la nueva lista)
                var tagsToDeactivate = currentActiveTags
                    .Where(pt => !newTagIds.Contains(pt.TagId))
                    .ToList();

                // Tags a activar/crear (están en la nueva lista pero no activos actualmente)
                var tagsToActivateIds = newTagIds
                    .Where(tagId => !currentTagIds.Contains(tagId))
                    .ToList();

                // Tags que permanecen igual (no necesitan cambios)
                var unchangedTags = currentActiveTags
                    .Where(pt => newTagIds.Contains(pt.TagId))
                    .ToList();

                _logger.LogInformation(
                    "Cambios a aplicar - Desactivar: {DeactivateCount}, Activar/Crear: {ActivateCount}, Sin cambios: {UnchangedCount}",
                    tagsToDeactivate.Count, tagsToActivateIds.Count, unchangedTags.Count
                );

                // Aplicar cambios
                foreach (var productTag in tagsToDeactivate)
                {
                    productTag.IsActive = false;
                    _logger.LogDebug("Desactivando relación ProductId: {ProductId}, TagId: {TagId}", productTag.ProductId, productTag.TagId);
                }

                foreach (var tagId in tagsToActivateIds)
                {
                    var existingRelation = await _db.ProductTags
                        .FirstOrDefaultAsync(pt => pt.ProductId == id && pt.TagId == tagId);

                    if (existingRelation != null)
                    {
                        // Reactivar relación existente
                        existingRelation.IsActive = true;
                        _logger.LogDebug("Reactivando relación existente ProductId: {ProductId}, TagId: {TagId}", id, tagId);
                    }
                    else
                    {
                        // Crear nueva relación
                        var newProductTag = new ProductTag
                        {
                            ProductId = id,
                            TagId = tagId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.ProductTags.Add(newProductTag);
                        _logger.LogDebug("Creando nueva relación ProductId: {ProductId}, TagId: {TagId}", id, tagId);
                    }
                }

                // Guardar cambios
                var changesSaved = await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Tags guardados exitosamente para producto ID: {ProductId}. Cambios aplicados: {ChangesSaved}. Tags finales: {FinalTagCount}",
                    id, changesSaved, newTagIds.Count
                );

                // Devolver información útil para el cliente
                return Json(new
                {
                    ok = true,
                    message = "Etiquetas guardadas correctamente",
                    stats = new
                    {
                        totalTags = newTagIds.Count,
                        deactivated = tagsToDeactivate.Count,
                        activated = tagsToActivateIds.Count,
                        unchanged = unchangedTags.Count
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(dbEx, "Error de base de datos al guardar tags para producto ID: {ProductId}", id);

                // Detectar violación de constraints únicas
                if (dbEx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Json(new { error = "Error de duplicación en las relaciones de etiquetas" });
                }

                return Json(new { error = "Error de base de datos al guardar las etiquetas" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al guardar tags para producto ID: {ProductId}", id);
                return Json(new { error = "Error inesperado al guardar las etiquetas" });
            }
        }

        private async Task<string?> SaveProductImageAsync(int productId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Formato de archivo no permitido.");

            // Crear directorio si no existe
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products", productId.ToString());
            Directory.CreateDirectory(uploadsPath);

            // Generar nombre único para el archivo
            var fileName = $"main_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(uploadsPath, fileName);

            // Guardar archivo
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retornar ruta relativa para la web
            return $"/uploads/products/{productId}/{fileName}";
        }

        // Método para garantizar slugs únicos
        private async Task<string> EnsureUniqueProductSlugAsync(string baseSlug, int? ignoreId = null)
        {
            // Si el slug base está vacío (por ejemplo, nombre con solo caracteres especiales), usar "producto"
            string slug = string.IsNullOrWhiteSpace(baseSlug) ? "producto" : baseSlug;
            int suffix = 1;
            var originalSlug = slug;

            while (await _db.Products.AnyAsync(p => p.Slug == slug && (!ignoreId.HasValue || p.Id != ignoreId.Value)))
            {
                slug = $"{originalSlug}-{suffix++}";
            }
            return slug;
        }

        
// Agregar estos métodos a ProductsController.cs
// Agregar estos métodos a ProductsController.cs

[Authorize(Roles = "Admin,Employee")]
[HttpGet("/Products/{productId:int}/Variants")]
public async Task<IActionResult> Variants(int productId)
{
    try
    {
        if (productId <= 0)
        {
            TempData["Error"] = "ID de producto inválido.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _db.Products
            .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            TempData["Error"] = "Producto no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var variants = product.Variants
            .OrderBy(v => v.Fit)
            .ThenBy(v => v.Color)
            .ThenBy(v => v.Size)
            .ToList();

        var vm = new TiendaPlayeras.Web.Models.ViewModels.VariantsAdminViewModel
        {
            Product = product,
            Variants = variants,
            PredefinedFits = new[] { "Hombre", "Mujer", "Unisex", "Oversize", "Boxy" },
            PredefinedSizes = new[] { "XS", "S", "M", "L", "XL", "XXL", "Yasui" },
            PredefinedColors = new[] { "Negro", "Blanco", "Rojo", "Azul", "Verde", "Gris", "Amarillo", "Rosa" }
        };

        return View("~/Views/Variants/Index.cshtml", vm);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al cargar variantes del producto ID: {ProductId}", productId);
        TempData["Error"] = "Error al cargar las variantes del producto.";
        return RedirectToAction(nameof(Index));
    }
}

// NUEVA ACCIÓN: Lista general de productos con variantes para acceso rápido
[Authorize(Roles = "Admin,Employee")]
[HttpGet("/Products/VariantsManager")]
public async Task<IActionResult> VariantsManager()
{
    try
    {
        var products = await _db.Products
            .Include(p => p.Variants)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.MainImagePath,
                VariantsCount = p.Variants.Count(v => v.IsActive),
                TotalVariants = p.Variants.Count
            })
            .ToListAsync();

        return View("~/Views/Variants/Manager.cshtml", products);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al cargar el gestor de variantes");
        TempData["Error"] = "Error al cargar el gestor de variantes.";
        return RedirectToAction(nameof(Index));
    }
}

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Employee")]
public async Task<IActionResult> SaveVariantOptions(
    int productId,
    bool useFit,
    bool useColor,
    bool useSize,
    string? allowedFitsCsv,
    string? allowedColorsCsv,
    string? allowedSizesCsv)
{
    try
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null)
        {
            TempData["Error"] = "Producto no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        // Normalizar CSV: eliminar espacios y comas extras
        string? CleanCsv(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            
            var items = input
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToArray();
            
            return items.Any() ? string.Join(",", items) : null;
        }

        product.UseFit = useFit;
        product.UseColor = useColor;
        product.UseSize = useSize;
        product.AllowedFitsCsv = CleanCsv(allowedFitsCsv);
        product.AllowedColorsCsv = CleanCsv(allowedColorsCsv);
        product.AllowedSizesCsv = CleanCsv(allowedSizesCsv);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Configuración de variantes actualizada correctamente.";
        return RedirectToAction(nameof(Variants), new { productId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al guardar opciones de variantes para ProductId: {ProductId}", productId);
        TempData["Error"] = "Error al guardar la configuración de variantes.";
        return RedirectToAction(nameof(Variants), new { productId });
    }
}

[Authorize(Roles = "Admin,Employee")]
[HttpGet("/Products/{productId:int}/Variants/Create")]
public async Task<IActionResult> CreateVariant(int productId)
{
    var product = await _db.Products.FindAsync(productId);
    if (product == null)
    {
        TempData["Error"] = "Producto no encontrado.";
        return RedirectToAction(nameof(Index));
    }

    ViewBag.ProductName = product.Name;
    return View("~/Views/Variants/Create.cshtml", new ProductVariant 
    { 
        ProductId = productId, 
        IsActive = true,
        Price = product.BasePrice
    });
}

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Employee")]
public async Task<IActionResult> CreateVariant(ProductVariant model)
{
    try
    {
        if (string.IsNullOrWhiteSpace(model.Fit))
            ModelState.AddModelError(nameof(model.Fit), "El fit es requerido.");
        
        if (string.IsNullOrWhiteSpace(model.Color))
            ModelState.AddModelError(nameof(model.Color), "El color es requerido.");
        
        if (string.IsNullOrWhiteSpace(model.Size))
            ModelState.AddModelError(nameof(model.Size), "La talla es requerida.");

        if (model.Price < 0)
            ModelState.AddModelError(nameof(model.Price), "El precio no puede ser negativo.");

        if (model.Stock < 0)
            ModelState.AddModelError(nameof(model.Stock), "El stock no puede ser negativo.");

        // Verificar si ya existe una variante con la misma combinación
        var existingVariant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => 
                v.ProductId == model.ProductId &&
                v.Fit.ToLower() == model.Fit.Trim().ToLower() &&
                v.Color.ToLower() == model.Color.Trim().ToLower() &&
                v.Size.ToLower() == model.Size.Trim().ToLower());

        if (existingVariant != null)
        {
            ModelState.AddModelError("", "Ya existe una variante con esta combinación de Fit, Color y Talla.");
        }

        if (!ModelState.IsValid)
        {
            var product = await _db.Products.FindAsync(model.ProductId);
            ViewBag.ProductName = product?.Name;
            return View("~/Views/Variants/Create.cshtml", model);
        }

        // Normalizar valores
        model.Fit = model.Fit.Trim();
        model.Color = model.Color.Trim();
        model.Size = model.Size.Trim();
        model.DesignCode = model.DesignCode?.Trim() ?? "";

        _db.ProductVariants.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Variante {model.Fit} - {model.Color} - {model.Size} creada correctamente.";
        return RedirectToAction(nameof(Variants), new { productId = model.ProductId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al crear variante para producto ID: {ProductId}", model.ProductId);
        ModelState.AddModelError("", "Error al guardar la variante.");
        
        var product = await _db.Products.FindAsync(model.ProductId);
        ViewBag.ProductName = product?.Name;
        return View("~/Views/Variants/Create.cshtml", model);
    }
}

[Authorize(Roles = "Admin,Employee")]
[HttpGet("/Products/Variants/Edit/{id:int}")]
public async Task<IActionResult> EditVariant(int id)
{
    var variant = await _db.ProductVariants
        .Include(v => v.Product)
        .FirstOrDefaultAsync(v => v.Id == id);

    if (variant == null)
    {
        TempData["Error"] = "Variante no encontrada.";
        return RedirectToAction(nameof(Index));
    }

    ViewBag.ProductName = variant.Product?.Name;
    return View("~/Views/Variants/Edit.cshtml", variant);
}

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Employee")]
public async Task<IActionResult> EditVariant(int id, ProductVariant model)
{
    try
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (variant == null)
        {
            TempData["Error"] = "Variante no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(model.Fit))
            ModelState.AddModelError(nameof(model.Fit), "El fit es requerido.");
        
        if (string.IsNullOrWhiteSpace(model.Color))
            ModelState.AddModelError(nameof(model.Color), "El color es requerido.");
        
        if (string.IsNullOrWhiteSpace(model.Size))
            ModelState.AddModelError(nameof(model.Size), "La talla es requerida.");

        // Verificar duplicados (excluyendo la variante actual)
        var duplicateVariant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => 
                v.Id != id &&
                v.ProductId == variant.ProductId &&
                v.Fit.ToLower() == model.Fit.Trim().ToLower() &&
                v.Color.ToLower() == model.Color.Trim().ToLower() &&
                v.Size.ToLower() == model.Size.Trim().ToLower());

        if (duplicateVariant != null)
        {
            ModelState.AddModelError("", "Ya existe otra variante con esta combinación.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProductName = variant.Product?.Name;
            return View("~/Views/Variants/Edit.cshtml", model);
        }

        variant.Fit = model.Fit.Trim();
        variant.Color = model.Color.Trim();
        variant.Size = model.Size.Trim();
        variant.DesignCode = model.DesignCode?.Trim() ?? "";
        variant.Price = model.Price;
        variant.Stock = model.Stock;
        variant.IsActive = model.IsActive;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Variante actualizada correctamente.";
        return RedirectToAction(nameof(Variants), new { productId = variant.ProductId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al editar variante ID: {VariantId}", id);
        ModelState.AddModelError("", "Error al actualizar la variante.");
        
        var variant = await _db.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == id);
        ViewBag.ProductName = variant?.Product?.Name;
        return View("~/Views/Variants/Edit.cshtml", model);
    }
}

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Employee")]
public async Task<IActionResult> ToggleVariant(int id)
{
    try
    {
        var variant = await _db.ProductVariants.FindAsync(id);
        if (variant == null)
        {
            TempData["Error"] = "Variante no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        variant.IsActive = !variant.IsActive;
        await _db.SaveChangesAsync();

        var action = variant.IsActive ? "habilitada" : "inhabilitada";
        TempData["Success"] = $"Variante {action} correctamente.";
        
        return RedirectToAction(nameof(Variants), new { productId = variant.ProductId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al cambiar estado de variante ID: {VariantId}", id);
        TempData["Error"] = "Error al cambiar el estado de la variante.";
        return RedirectToAction(nameof(Index));
    }
}

    // Detalle público por slug: /p/{slug}
    [AllowAnonymous]
    [HttpGet("/p/{slug}")]
    public async Task<IActionResult> Product(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var product = await _db.Products
            .Include(p => p.Variants)
            .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag) // por si quieres mostrar tags
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product == null) return NotFound();

        // Solo variantes activas
        product.Variants = product.Variants?.Where(v => v.IsActive).ToList() ?? new();

        return View("Product", product);
    }
        // ELIMINÉ LAS ACCIONES DUPLICADAS:
        // - Create(Product model) sin imagen
        // - Edit(Product model) sin imagen
        // - El método Slugify() antiguo fue reemplazado
    }
}