using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET /Wishlist/Count  (lo usa el badge del header)
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });

            var count = await _db.WishlistItems
                .CountAsync(w => w.UserId == user.Id && w.IsActive);

            return Json(new { count });
        }

        // GET /Wishlist/Drawer  (inyecta el partial en el modal)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Drawer()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _db.WishlistItems
                .Include(x => x.Product)!.ThenInclude(p => p!.ProductImages)
                .Where(x => x.UserId == user!.Id && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return PartialView("~/Views/Wishlist/_WishlistDrawer.cshtml", items);
        }

        // POST /Wishlist/Add  (agrega por productId)
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddRemoveDto dto)
        {
            if (dto == null || dto.ProductId <= 0) return BadRequest();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var exists = await _db.WishlistItems
                .AnyAsync(w => w.UserId == user.Id && w.ProductId == dto.ProductId && w.IsActive);

            if (!exists)
            {
                // valida que el producto exista
                var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId && p.IsActive);
                if (!productExists) return NotFound();

                _db.WishlistItems.Add(new WishlistItem
                {
                    UserId = user.Id,
                    ProductId = dto.ProductId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
            }

            var count = await _db.WishlistItems.CountAsync(w => w.UserId == user.Id && w.IsActive);
            return Json(new { ok = true, added = !exists, count });
        }

        // POST /Wishlist/Remove  (quita por productId)
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Remove([FromBody] AddRemoveDto dto)
        {
            if (dto == null || dto.ProductId <= 0) return BadRequest();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _db.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == dto.ProductId && w.IsActive);

            if (item != null)
            {
                _db.WishlistItems.Remove(item);
                await _db.SaveChangesAsync();
            }

            var count = await _db.WishlistItems.CountAsync(w => w.UserId == user.Id && w.IsActive);
            return Json(new { ok = true, removed = item != null, count });
        }

        public class AddRemoveDto { public int ProductId { get; set; } }
    }
}
