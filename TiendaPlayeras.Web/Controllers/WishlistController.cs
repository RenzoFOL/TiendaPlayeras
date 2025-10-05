using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;


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
            var items = await _db.WishlistItems.Include(w => w.ProductVariant).ThenInclude(v => v.Product)
            .Where(w => w.UserId == uid && w.IsActive).ToListAsync();
            return View(items);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int variantId)
        {
            var uid = User.GetUserId();
            var exists = await _db.WishlistItems.AnyAsync(w => w.UserId == uid && w.ProductVariantId == variantId && w.IsActive);
            if (!exists)
            {
                _db.WishlistItems.Add(new Models.WishlistItem { UserId = uid, ProductVariantId = variantId, IsActive = true });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _db.WishlistItems.FindAsync(id);
            if (item != null) { item.IsActive = false; await _db.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }
    }
}