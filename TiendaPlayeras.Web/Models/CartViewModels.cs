// TiendaPlayeras.Web/Models/CartViewModels.cs
namespace TiendaPlayeras.Web.Models
{
    public class CartLine
    {
        public int CartItemId { get; set; }
        public int ProductId  { get; set; }
        public string ProductName { get; set; } = "";
        public string Size     { get; set; } = "";
        public int Quantity    { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class CartSummary
    {
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartLine> Lines { get; set; } = new();
    }
}
