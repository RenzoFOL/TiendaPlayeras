using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>Lista de deseos: solo clientes autenticados.</summary>
    [Authorize(Roles = "Customer")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        public WishlistController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var uid = User.GetUserId();
            // ✅ ACTUALIZADO: Incluir Product y sus imágenes
            var items = await _db.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p!.ProductImages)
                .Where(w => w.UserId == uid && w.IsActive)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var uid = User.GetUserId();
            
            // ✅ ACTUALIZADO: Verificar por ProductId (no variantId)
            var exists = await _db.WishlistItems
                .AnyAsync(w => w.UserId == uid && w.ProductId == productId && w.IsActive);
                
            if (!exists)
            {
                _db.WishlistItems.Add(new WishlistItem 
                { 
                    UserId = uid, 
                    ProductId = productId, 
                    IsActive = true 
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Producto agregado a tu lista de deseos";
            }
            else
            {
                TempData["Info"] = "El producto ya está en tu lista de deseos";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _db.WishlistItems.FindAsync(id);
            if (item != null) 
            { 
                item.IsActive = false; 
                await _db.SaveChangesAsync();
                TempData["Success"] = "Producto removido de tu lista de deseos";
            }
            return RedirectToAction("Index");
        }

        // ✅ NUEVO: Método para agregar desde AJAX
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            try
            {
                var uid = User.GetUserId();
                var existingItem = await _db.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == uid && w.ProductId == productId && w.IsActive);

                if (existingItem != null)
                {
                    // Remover de wishlist
                    existingItem.IsActive = false;
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, added = false, message = "Removido de tu lista de deseos" });
                }
                else
                {
                    // Agregar a wishlist
                    _db.WishlistItems.Add(new WishlistItem 
                    { 
                        UserId = uid, 
                        ProductId = productId, 
                        IsActive = true 
                    });
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, added = true, message = "Agregado a tu lista de deseos" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ✅ NUEVO: Obtener conteo de wishlist para mostrar en layout
        [HttpGet]
        public async Task<IActionResult> GetWishlistCount()
        {
            var uid = User.GetUserId();
            var count = await _db.WishlistItems
                .CountAsync(w => w.UserId == uid && w.IsActive);
            return Json(new { count });
        }
    }
}