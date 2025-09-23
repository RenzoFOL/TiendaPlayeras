using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Services
{
    /// <summary>
    /// Implementación de carrito usando EF Core. Maneja SessionId (invitado) y UserId (cliente).
    /// </summary>
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        public CartService(ApplicationDbContext db) => _db = db;


        public async Task<int> GetOrCreateCartIdAsync(string? userId, string sessionId)
        {
            // Buscar carrito activo por usuario o por sesión
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.IsActive &&
            ((userId != null && c.UserId == userId) || (userId == null && c.SessionId == sessionId)));
            // Crear si no existe
            if (cart == null)
            {
                cart = new Cart { UserId = userId, SessionId = userId == null ? sessionId : string.Empty, IsActive = true };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }
            return cart.Id;
        }


        public async Task AddAsync(int cartId, int productVariantId, int quantity, decimal unitPrice)
        {
            // Busca ítem existente para acumular cantidad
            var item = await _db.CartItems.FirstOrDefaultAsync(i => i.IsActive && i.CartId == cartId && i.ProductVariantId == productVariantId);
            if (item == null)
            {
                item = new CartItem { CartId = cartId, ProductVariantId = productVariantId, Quantity = quantity, UnitPrice = unitPrice, IsActive = true };
                _db.CartItems.Add(item);
            }
            else
            {
                item.Quantity += quantity;
                _db.CartItems.Update(item);
            }
            await _db.SaveChangesAsync();
        }


        public async Task RemoveAsync(int cartItemId)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                item.IsActive = false; // inhabilitación lógica
                _db.CartItems.Update(item);
                await _db.SaveChangesAsync();
            }
        }


        public async Task ClearAsync(int cartId)
        {
            var items = await _db.CartItems.Where(i => i.CartId == cartId && i.IsActive).ToListAsync();
            foreach (var i in items) i.IsActive = false;
            await _db.SaveChangesAsync();
        }


        public async Task<(decimal subtotal, int totalItems)> GetSummaryAsync(int cartId)
        {
            var q = _db.CartItems.Where(i => i.CartId == cartId && i.IsActive);
            var subtotal = await q.SumAsync(i => i.UnitPrice * i.Quantity);
            var totalItems = await q.SumAsync(i => i.Quantity);
            return (subtotal, totalItems);
        }


        public async Task MergeGuestCartToUserAsync(string sessionId, string userId)
        {
            // Une el carrito de invitado a uno de usuario (llamar tras login)
            var guest = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.IsActive && c.SessionId == sessionId && c.UserId == null);
            if (guest == null) return;
        }
    }
}