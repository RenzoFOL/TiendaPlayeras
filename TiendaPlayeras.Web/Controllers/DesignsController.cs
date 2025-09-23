using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Controllers
{
/// <summary>Subida de diseños personalizados (cliente autenticado).</summary>
[Authorize(Roles = "Customer")]
public class DesignsController : Controller
{
private readonly ApplicationDbContext _db;
public DesignsController(ApplicationDbContext db) => _db = db;


public IActionResult Upload() => View();


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Upload(IFormFile file)
{
if (file == null || file.Length == 0) { ModelState.AddModelError("file", "Archivo requerido"); return View(); }
var allowed = new[] { "application/pdf", "image/png", "image/svg+xml", "image/jpeg" };
if (!allowed.Contains(file.ContentType)) { ModelState.AddModelError("file", "Formato no permitido"); return View(); }
if (file.Length > 10 * 1024 * 1024) { ModelState.AddModelError("file", "Máximo 10 MB"); return View(); }


var folder = Path.Combine("wwwroot", "uploads", "designs", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
Directory.CreateDirectory(folder);
var ext = Path.GetExtension(file.FileName);
var name = $"{Guid.NewGuid()}{ext}";
var path = Path.Combine(folder, name);
using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);


var entity = new DesignUpload
{
UserId = User.GetUserId()!,
FileName = file.FileName,
StoragePath = Path.Combine("/uploads/designs", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), name),
FileSizeBytes = file.Length,
ContentType = file.ContentType,
IsActive = true
};
_db.DesignUploads.Add(entity);
await _db.SaveChangesAsync();


TempData["ok"] = "Diseño subido";
return RedirectToAction("MyDesigns");
}


public IActionResult MyDesigns()
{
var uid = User.GetUserId();
var list = _db.DesignUploads.Where(d => d.UserId == uid && d.IsActive).OrderByDescending(d => d.UploadedAt).ToList();
return View(list);
}
}
}