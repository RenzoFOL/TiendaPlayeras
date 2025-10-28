using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using Slugify;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ProductsController> _logger;
        private readonly SlugHelper _slugHelper;

        public ProductsController(ApplicationDbContext db, ILogger<ProductsController> logger)
        {
            _db = db;
            _logger = logger;
            _slugHelper = new SlugHelper();
        }

        // GET /Products
        [HttpGet("/Products")]
        public async Task<IActionResult> Index(string? q, string? tag, string sort = "az", int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _db.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductTags)
                        .ThenInclude(pt => pt.Tag)
                            .ThenInclude(t => t!.Category)
                    .AsQueryable();

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

                query = sort?.ToLower() switch
                {
                    "za" => query.OrderByDescending(p => p.Name),
                    "price_asc" => query.OrderBy(p => p.BasePrice),
                    "price_desc" => query.OrderByDescending(p => p.BasePrice),
                    "id_asc" => query.OrderBy(p => p.Id),
                    "id_desc" => query.OrderByDescending(p => p.Id),
                    _ => query.OrderBy(p => p.Name)
                };

                var total = await query.CountAsync();
                pageSize = Math.Clamp(pageSize, 10, 100);
                page = Math.Max(1, page);

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.Total = total;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Q = q ?? "";
                ViewBag.Tag = tag ?? "";
                ViewBag.Sort = sort ?? "az";

                return View("Index", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar lista de productos");
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

        // POST /Products/Create - ACTUALIZADO
[HttpPost("/Products/Create")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Product model)
{
    try
    {
        _logger.LogInformation("🔄 Iniciando creación de producto: {ProductName}", model.Name);

        // Procesar tallas disponibles
        // ✅ NUEVO: Procesar tallas como booleanos
        model.SizeS = Request.Form["sizeS"] == "true";
        model.SizeM = Request.Form["sizeM"] == "true";
        model.SizeL = Request.Form["sizeL"] == "true";
        model.SizeXL = Request.Form["sizeXL"] == "true";
        model.UpdateAvailableSizes(); // Mantener compatibilidad con AvailableSizes

        _logger.LogInformation("📏 Tallas procesadas - S: {S}, M: {M}, L: {L}, XL: {XL}", 
        model.SizeS, model.SizeM, model.SizeL, model.SizeXL);

        await ValidateProductModelAsync(model, null);

        var images = Request.Form.Files.Where(f => f.Name == "images").ToList();
        _logger.LogInformation("📸 Archivos recibidos: {FileCount}", images.Count);

        ValidateUploadedImages(images);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("❌ ModelState inválido: {Errors}", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return View("Create", model);
        }

        model.Slug = await EnsureUniqueProductSlugAsync(_slugHelper.GenerateSlug(model.Name));
        model.CreatedAt = DateTime.UtcNow;
        model.IsActive = true;

        _db.Products.Add(model);
        await _db.SaveChangesAsync();

        _logger.LogInformation("✅ Producto creado ID: {ProductId}", model.Id);

        // ✅ CORREGIDO: Manejar errores en guardado de imágenes sin afectar el producto
        if (images.Any())
        {
            try
            {
                _logger.LogInformation("🖼️ Guardando {Count} imágenes para producto {ProductId}", images.Count, model.Id);
                await SaveProductImagesAsync(model.Id, images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error guardando imágenes, pero el producto se creó correctamente");
                // No lanzar excepción - el producto ya se creó
                TempData["Warning"] = "Producto creado, pero hubo un error guardando algunas imágenes.";
                return RedirectToAction("Edit", new { id = model.Id });
            }
        }
        else
        {
            _logger.LogWarning("⚠️ No hay imágenes para guardar");
        }

        TempData["Success"] = $"Producto '{model.Name}' creado correctamente.";
        return RedirectToAction("Edit", new { id = model.Id });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error al crear producto");
        ModelState.AddModelError("", "Error al crear el producto.");
        return View("Create", model);
    }
}

        // GET /Products/Edit/5
        [HttpGet("/Products/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    TempData["Error"] = "Producto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                return View("Edit", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar producto para editar");
                TempData["Error"] = "Error al cargar el producto.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SaveProductWithTags - ACTUALIZADO
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> SaveProductWithTags(int id, Product model, [FromForm] int[] tagIds)
{
    using var transaction = await _db.Database.BeginTransactionAsync();

    try
    {
        var existing = await _db.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing == null)
        {
            TempData["Error"] = "Producto no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ CORREGIDO: Manejar availableSizes cuando es null
        existing.SizeS = Request.Form["sizeS"] == "true";
        existing.SizeM = Request.Form["sizeM"] == "true"; 
        existing.SizeL = Request.Form["sizeL"] == "true";
        existing.SizeXL = Request.Form["sizeXL"] == "true";
        existing.UpdateAvailableSizes(); // Mantener compatibilidad

        _logger.LogInformation("📏 Tallas actualizadas - S: {S}, M: {M}, L: {L}, XL: {XL}", 
        existing.SizeS, existing.SizeM, existing.SizeL, existing.SizeXL);

        await ValidateProductModelAsync(model, id);

        var newImages = Request.Form.Files.Where(f => f.Name == "images").ToList();
        ValidateUploadedImages(newImages);

        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        // Actualizar propiedades del producto
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.BasePrice = model.BasePrice;
        existing.IsActive = model.IsActive;
        existing.IsCustomizable = model.IsCustomizable;
        existing.Slug = await EnsureUniqueProductSlugAsync(_slugHelper.GenerateSlug(model.Name), id);
        existing.UpdatedAt = DateTime.UtcNow;

        // Guardar nuevas imágenes
        if (newImages.Any())
        {
            _logger.LogInformation("📸 Guardando {Count} imágenes nuevas", newImages.Count);
            await SaveProductImagesAsync(id, newImages);
        }

        await _db.SaveChangesAsync();

        // Actualizar tags
        await UpdateProductTagsAsync(id, tagIds);

        await transaction.CommitAsync();

        TempData["Success"] = $"Producto '{existing.Name}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error al guardar producto");
        ModelState.AddModelError("", "Error al guardar los cambios.");
        return View("Edit", model);
    }
}

        // POST: ToggleActive
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

        // GET: Obtener tags
        [HttpGet]
        public async Task<IActionResult> GetTags(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { error = "ID inválido" });
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

                return Json(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tags");
                return Json(new { error = "Error al cargar las etiquetas" });
            }
        }

        // POST: SaveTags
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTags(int id, [FromForm] int[] tagIds)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Iniciando guardado de tags para producto ID: {ProductId}, tags recibidos: {TagCount}", id, tagIds?.Length ?? 0);

                if (id <= 0)
                {
                    _logger.LogWarning("ID de producto inválido: {ProductId}", id);
                    return Json(new { error = "ID de producto inválido" });
                }

                var product = await _db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    _logger.LogWarning("Producto no encontrado ID: {ProductId}", id);
                    return Json(new { error = "Producto no encontrado" });
                }

                await UpdateProductTagsAsync(id, tagIds);
                await transaction.CommitAsync();

                return Json(new
                {
                    ok = true,
                    message = "Etiquetas guardadas correctamente"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al guardar tags para producto ID: {ProductId}", id);
                return Json(new { error = "Error inesperado al guardar las etiquetas" });
            }
        }

        // Vista pública de producto
        [AllowAnonymous]
        [HttpGet("/Catalog/Product")]
        public async Task<IActionResult> Product(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var product = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null) return NotFound();

            return View("~/Views/Catalog/Product.cshtml", product);
        }

        // MÉTODOS AUXILIARES PRIVADOS

        private async Task ValidateProductModelAsync(Product model, int? existingId)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "El nombre es obligatorio.");
            }
            else
            {
                var query = _db.Products.Where(p => p.Name.ToLower() == model.Name.Trim().ToLower());
                if (existingId.HasValue)
                {
                    query = query.Where(p => p.Id != existingId.Value);
                }

                var exists = await query.FirstOrDefaultAsync();
                if (exists != null)
                {
                    ModelState.AddModelError(nameof(model.Name), "Ya existe un producto con este nombre.");
                }
            }

            if (model.BasePrice < 0)
            {
                ModelState.AddModelError(nameof(model.BasePrice), "El precio no puede ser negativo.");
            }
        }

        private void ValidateUploadedImages(List<IFormFile> images)
        {
            if (images.Any())
            {
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
                foreach (var img in images)
                {
                    if (!allowedTypes.Contains(img.ContentType))
                    {
                        ModelState.AddModelError("", $"Formato no válido: {img.FileName}");
                    }
                    if (img.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", $"Archivo muy grande: {img.FileName}");
                    }
                }
            }
        }

private async Task SaveProductImagesAsync(int productId, List<IFormFile> files)
{
    try
    {
        _logger.LogInformation("🔄 Iniciando guardado de imágenes para producto {ProductId}, archivos: {FileCount}", productId, files?.Count ?? 0);

        // ✅ CORREGIDO: Verificación más robusta
        if (files == null || !files.Any())
        {
            _logger.LogWarning("❌ No hay archivos para guardar");
            return;
        }

        // ✅ CORREGIDO: Construir ruta de manera segura
        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsPath = Path.Combine(wwwrootPath, "uploads", "products", productId.ToString());
        
        _logger.LogInformation("📁 Ruta de upload: {UploadPath}", uploadsPath);

        // ✅ CORREGIDO: Crear directorio de manera segura
        try
        {
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                _logger.LogInformation("✅ Directorio creado: {UploadPath}", uploadsPath);
            }
            else
            {
                _logger.LogInformation("✅ Directorio ya existe: {UploadPath}", uploadsPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creando directorio: {UploadPath}", uploadsPath);
            throw;
        }

        // ✅ CORREGIDO: Obtener último orden de manera segura
        int lastOrder = -1;
        try
        {
            lastOrder = await _db.ProductImages
                .Where(i => i.ProductId == productId)
                .Select(i => i.DisplayOrder)
                .DefaultIfEmpty(-1)
                .MaxAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error obteniendo último orden, usando -1");
            lastOrder = -1;
        }

        int order = lastOrder + 1;
        _logger.LogInformation("📊 Último orden: {LastOrder}, nuevo orden: {NewOrder}", lastOrder, order);

        int savedCount = 0;
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        foreach (var file in files)
        {
            try
            {
                _logger.LogInformation("📄 Procesando archivo: {FileName} ({FileSize} bytes)", file.FileName, file.Length);

                // ✅ CORREGIDO: Verificaciones más robustas
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("⚠️ Archivo nulo o vacío: {FileName}", file?.FileName ?? "null");
                    continue;
                }

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("❌ Formato no permitido: {FileName} ({Extension})", file.FileName, ext);
                    continue;
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("❌ Archivo muy grande: {FileName} ({FileSize} bytes)", file.FileName, file.Length);
                    continue;
                }

                // ✅ CORREGIDO: Generar nombre de archivo seguro
                var fileName = $"img_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsPath, fileName);
                
                _logger.LogInformation("💾 Guardando como: {FileName} en {FullPath}", fileName, fullPath);

                // ✅ CORREGIDO: Guardar archivo físico
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // ✅ CORREGIDO: Verificar que el archivo se creó
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogError("❌ El archivo no se creó: {FullPath}", fullPath);
                    continue;
                }

                var fileInfo = new FileInfo(fullPath);
                _logger.LogInformation("📏 Archivo creado - Tamaño: {Size} bytes", fileInfo.Length);

                // ✅ CORREGIDO: Crear ruta relativa
                var relativePath = $"/uploads/products/{productId}/{fileName}";

                // ✅ CORREGIDO: Crear objeto ProductImage de manera segura
                var productImage = new ProductImage
                {
                    ProductId = productId,
                    Path = relativePath,
                    DisplayOrder = order,
                    IsMain = (order == 0), // La primera imagen (orden 0) será principal
                    AltText = Path.GetFileNameWithoutExtension(file.FileName) ?? $"Imagen {order + 1}",
                    CreatedAt = DateTime.UtcNow
                };

                _db.ProductImages.Add(productImage);
                savedCount++;
                order++; // Incrementar orden para la siguiente imagen

                _logger.LogInformation("✅ Imagen agregada a BD: {RelativePath}", relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando archivo: {FileName}", file?.FileName ?? "unknown");
                // Continuar con el siguiente archivo
            }
        }

        // ✅ CORREGIDO: Guardar en BD solo si hay imágenes
        if (savedCount > 0)
        {
            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("💾 {Count} imágenes guardadas en BD para producto {ProductId}", savedCount, productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error guardando imágenes en BD");
                throw;
            }
        }
        else
        {
            _logger.LogWarning("⚠️ No se guardaron imágenes en la BD");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error crítico en SaveProductImagesAsync para producto {ProductId}", productId);
        throw;
    }
}

        private async Task UpdateProductTagsAsync(int productId, int[]? tagIds)
        {
            var validTagIds = tagIds?.Where(t => t > 0).Distinct().ToArray() ?? Array.Empty<int>();

            if (validTagIds.Any())
            {
                var existingTags = await _db.Tags
                    .Where(t => validTagIds.Contains(t.Id) && t.IsActive)
                    .Select(t => t.Id)
                    .ToListAsync();

                validTagIds = existingTags.ToArray();
            }

            var currentTags = await _db.ProductTags
                .Where(pt => pt.ProductId == productId && pt.IsActive)
                .ToListAsync();

            var currentIds = currentTags.Select(pt => pt.TagId).ToHashSet();
            var newIds = validTagIds.ToHashSet();

            // Desactivar tags removidos
            foreach (var pt in currentTags.Where(pt => !newIds.Contains(pt.TagId)))
            {
                pt.IsActive = false;
            }

            // Activar/crear tags nuevos
            foreach (var tagId in newIds.Where(tid => !currentIds.Contains(tid)))
            {
                var relation = await _db.ProductTags
                    .FirstOrDefaultAsync(pt => pt.ProductId == productId && pt.TagId == tagId);

                if (relation != null)
                {
                    relation.IsActive = true;
                }
                else
                {
                    _db.ProductTags.Add(new ProductTag
                    {
                        ProductId = productId,
                        TagId = tagId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        // GET: Obtener imágenes
        [HttpGet]
        public async Task<IActionResult> GetImages(int id)
        {
            try
            {
                var images = await _db.ProductImages
                    .Where(i => i.ProductId == id)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new
                    {
                        id = i.Id,
                        path = i.Path,
                        isMain = i.IsMain,
                        displayOrder = i.DisplayOrder
                    })
                    .ToListAsync();

                return Json(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener imágenes");
                return Json(new { error = "Error al cargar las imágenes" });
            }
        }

        // POST: Eliminar imagen
        // POST: Eliminar imagen
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteImage(int id)
{
    try
    {
        var image = await _db.ProductImages.FindAsync(id);
        if (image == null)
        {
            return Json(new { success = false, error = "Imagen no encontrada" });
        }

        var productId = image.ProductId;

        // Eliminar archivo físico
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Path.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        // Si era la imagen principal, marcar la siguiente como principal
        if (image.IsMain)
        {
            var nextImage = await _db.ProductImages
                .Where(i => i.ProductId == productId && i.Id != id)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefaultAsync();

            if (nextImage != null)
            {
                nextImage.IsMain = true;
            }
        }

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al eliminar imagen");
        return Json(new { success = false, error = "Error al eliminar la imagen" });
    }
}

        // POST: Marcar imagen como principal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMainImage(int id)
        {
            try
            {
                var image = await _db.ProductImages.FindAsync(id);
                if (image == null)
                {
                    return Json(new { success = false, error = "Imagen no encontrada" });
                }

                var productId = image.ProductId;

                // Quitar principal de todas
                var allImages = await _db.ProductImages
                    .Where(i => i.ProductId == productId)
                    .ToListAsync();

                foreach (var img in allImages)
                {
                    img.IsMain = false;
                }

                // Marcar esta como principal
                image.IsMain = true;
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar imagen como principal");
                return Json(new { success = false, error = "Error al actualizar la imagen principal" });
            }
        }

        // Método auxiliar para slugs únicos
        private async Task<string> EnsureUniqueProductSlugAsync(string baseSlug, int? ignoreId = null)
        {
            string slug = string.IsNullOrWhiteSpace(baseSlug) ? "producto" : baseSlug;
            int suffix = 1;
            var originalSlug = slug;

            while (await _db.Products.AnyAsync(p => p.Slug == slug && (!ignoreId.HasValue || p.Id != ignoreId.Value)))
            {
                slug = $"{originalSlug}-{suffix++}";
            }
            return slug;
        }

       
    }
}