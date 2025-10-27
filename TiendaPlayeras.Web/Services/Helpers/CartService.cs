using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Services
{

    // DTO para resumen del carrito
    public class CartSummary
    {
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartItemSummary> Items { get; set; } = new();
    }

    public class CartItemSummary
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Cart> GetOrCreateCartAsync(string? userId, string sessionId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items.Where(i => i.IsActive))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.ProductImages)
                .FirstOrDefaultAsync(c => 
                    (userId != null && c.UserId == userId) || 
                    (userId == null && c.SessionId == sessionId));

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    SessionId = sessionId,
                    IsActive = true
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<int> GetOrCreateCartIdAsync(string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return cart.Id;
        }

        public async Task<CartItem> AddToCartAsync(int productId, string size, int quantity, string? userId, string sessionId)
        {
            // Validar que el producto existe y la talla es válida
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null)
                throw new ArgumentException("Producto no encontrado");

            var availableSizes = product.AvailableSizesList;
            if (!availableSizes.Contains(size))
                throw new ArgumentException($"Talla no disponible: {size}");

            return await AddAsync(productId, size, quantity, product.BasePrice, userId, sessionId);
        }

        public async Task<CartItem> AddAsync(int productId, string size, int quantity, decimal unitPrice, string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);

            // Buscar si ya existe el mismo producto+talla en el carrito
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => 
                    ci.CartId == cart.Id && 
                    ci.ProductId == productId && 
                    ci.Size == size && 
                    ci.IsActive);

            if (existingItem != null)
            {
                // Actualizar cantidad
                existingItem.Quantity += quantity;
                existingItem.UnitPrice = unitPrice; // Actualizar precio por si cambió
            }
            else
            {
                // Crear nuevo item
                existingItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Size = size,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    IsActive = true
                };
                _context.CartItems.Add(existingItem);
            }

            await _context.SaveChangesAsync();
            return existingItem;
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId)
        {
            return await RemoveAsync(cartItemId);
        }

        public async Task<bool> RemoveAsync(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            item.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateQuantityAsync(int cartItemId, int quantity)
        {
            if (quantity <= 0) 
                return await RemoveAsync(cartItemId);

            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            item.Quantity = quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearAsync(int cartId)
        {
            var items = await _context.CartItems
                .Where(ci => ci.CartId == cartId && ci.IsActive)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Cart> GetCartWithDetailsAsync(string? userId, string sessionId)
        {
            return await _context.Carts
                .Include(c => c.Items.Where(i => i.IsActive))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.ProductImages)
                .FirstOrDefaultAsync(c => 
                    (userId != null && c.UserId == userId) || 
                    (userId == null && c.SessionId == sessionId)) ?? new Cart();
        }

        public async Task<CartSummary> GetSummaryAsync(int cartId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items.Where(i => i.IsActive))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.ProductImages)
                .FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart == null)
                return new CartSummary();

            var summary = new CartSummary
            {
                TotalItems = cart.Items.Sum(i => i.Quantity),
                TotalAmount = cart.Items.Sum(i => i.Subtotal),
                Items = cart.Items.Select(i => new CartItemSummary
                {
                    CartItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Producto no disponible",
                    Size = i.Size,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal,
                    ImageUrl = i.Product?.MainImagePath
                }).ToList()
            };

            return summary;
        }

        public async Task<bool> MergeGuestCartToUserAsync(string sessionId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Obtener carrito de invitado
                var guestCart = await _context.Carts
                    .Include(c => c.Items.Where(i => i.IsActive))
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);

                if (guestCart == null || !guestCart.Items.Any())
                {
                    await transaction.CommitAsync();
                    return true; // No hay nada que fusionar
                }

                // Obtener o crear carrito de usuario
                var userCart = await GetOrCreateCartAsync(userId, sessionId);

                // Fusionar items
                foreach (var guestItem in guestCart.Items)
                {
                    var existingItem = userCart.Items
                        .FirstOrDefault(i => i.ProductId == guestItem.ProductId && i.Size == guestItem.Size);

                    if (existingItem != null)
                    {
                        // Sumar cantidades
                        existingItem.Quantity += guestItem.Quantity;
                    }
                    else
                    {
                        // Agregar nuevo item
                        userCart.Items.Add(new CartItem
                        {
                            ProductId = guestItem.ProductId,
                            Size = guestItem.Size,
                            Quantity = guestItem.Quantity,
                            UnitPrice = guestItem.UnitPrice,
                            IsActive = true
                        });
                    }

                    // Marcar item de invitado como inactivo
                    guestItem.IsActive = false;
                }

                // Marcar carrito de invitado como inactivo
                guestCart.IsActive = false;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}