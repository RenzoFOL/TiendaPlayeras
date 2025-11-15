// TiendaPlayeras.Web/Services/CartService.cs
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        public CartService(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(string userId, int productId, string size, int qty = 1)
        {
            size = (size ?? "M").Trim().ToUpper();

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && c.Size == size);

            if (existing is not null)
            {
                existing.Quantity += Math.Max(1, qty);
            }
            else
            {
                var p = await _db.Products
                    .Include(x => x.ProductImages)
                    .FirstOrDefaultAsync(x => x.Id == productId);

                if (p == null)
                    throw new InvalidOperationException("Producto no encontrado.");

                var img = p.ProductImages?
                            .OrderBy(pi => pi.DisplayOrder)
                            .Select(pi => pi.Path)
                            .FirstOrDefault()
                          ?? p.MainImagePath
                          ?? "/images/placeholder.png";

                _db.CartItems.Add(new CartItem
                {
                    UserId      = userId,
                    ProductId   = productId,
                    Size        = size,
                    Quantity    = Math.Max(1, qty),
                    UnitPrice   = p.BasePrice,     // usa tu propiedad de precio real
                    ProductName = p.Name,
                    ImageUrl    = img
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<CartSummary> GetSummaryAsync(string userId)
        {
            var items = await _db.CartItems
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var lines = items.Select(i => new CartLine
            {
                CartItemId  = i.Id,
                ProductId   = i.ProductId,
                ProductName = i.ProductName ?? string.Empty,
                Size        = i.Size,
                Quantity    = i.Quantity,
                UnitPrice   = i.UnitPrice,
                ImageUrl    = i.ImageUrl
            }).ToList();

            return new CartSummary
            {
                TotalItems  = lines.Sum(l => l.Quantity),
                TotalAmount = lines.Sum(l => l.Subtotal),
                Lines       = lines
            };
        }

        public Task<int> CountAsync(string userId)
            => _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);

        public async Task UpdateQtyAsync(string userId, int cartItemId, int qty)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item == null) return;
            item.Quantity = Math.Max(1, qty);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveAsync(string userId, int cartItemId)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item == null) return;
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task ClearAsync(string userId)
        {
            var items = _db.CartItems.Where(c => c.UserId == userId);
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
    }
}
