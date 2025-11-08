// TiendaPlayeras.Web/Services/ICartService.cs
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Services
{
    public interface ICartService
    {
        Task AddAsync(string userId, int productId, string size, int qty = 1);
        Task<CartSummary> GetSummaryAsync(string userId);
        Task<int> CountAsync(string userId);
        Task UpdateQtyAsync(string userId, int cartItemId, int qty);
        Task RemoveAsync(string userId, int cartItemId);
        Task ClearAsync(string userId);
    }
}
