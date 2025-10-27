using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string? userId, int? addressId, List<CartItem> cartItems);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(string? userId, int? addressId, List<CartItem> cartItems)
        {
            if (!cartItems.Any())
                throw new ArgumentException("El carrito está vacío");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = userId,
                    ShippingAddressId = addressId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Calcular totales
                decimal subtotal = 0;

                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Size = cartItem.Size,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        IsActive = true
                    };

                    order.Items.Add(orderItem);
                    subtotal += orderItem.Subtotal;
                }

                order.Subtotal = subtotal;
                order.Shipping = CalculateShipping(subtotal);
                order.Total = subtotal + order.Shipping;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p!.ProductImages)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.IsActive);
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p!.ProductImages)
                .Where(o => o.UserId == userId && o.IsActive)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        private decimal CalculateShipping(decimal subtotal)
        {
            // Lógica de envío - envío gratis sobre $500
            return subtotal >= 500 ? 0 : 50;
        }
    }
}