namespace TiendaPlayeras.Web.Services
{
/// <summary>Operaciones sobre Carrito (usuario logueado o invitado por SessionId).</summary>
public interface ICartService
{
Task<int> GetOrCreateCartIdAsync(string? userId, string sessionId);
Task AddAsync(int cartId, int productVariantId, int quantity, decimal unitPrice);
Task RemoveAsync(int cartItemId);
Task ClearAsync(int cartId);
Task<(decimal subtotal, int totalItems)> GetSummaryAsync(int cartId);
Task MergeGuestCartToUserAsync(string sessionId, string userId);
}
}