using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Services
{
/// <summary>Operaciones sobre Carrito (usuario logueado o invitado por SessionId).</summary>
public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(string? userId, string sessionId);
    Task<CartItem> AddToCartAsync(int productId, string size, int quantity, string? userId, string sessionId);
    Task<bool> RemoveFromCartAsync(int cartItemId);
    Task<bool> UpdateQuantityAsync(int cartItemId, int quantity);
    Task<Cart> GetCartWithDetailsAsync(string? userId, string sessionId);
    
    // Métodos adicionales que necesitas
    Task<int> GetOrCreateCartIdAsync(string? userId, string sessionId);
    Task<CartItem> AddAsync(int productId, string size, int quantity, decimal unitPrice, string? userId, string sessionId);
    Task<bool> RemoveAsync(int cartItemId);
    Task<bool> ClearAsync(int cartId);
    Task<CartSummary> GetSummaryAsync(int cartId);
    Task<bool> MergeGuestCartToUserAsync(string sessionId, string userId);
}
}