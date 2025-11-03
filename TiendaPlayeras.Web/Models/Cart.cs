using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaPlayeras.Web.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // nulo para invitado
        public ApplicationUser? User { get; set; }
        public string SessionId { get; set; } = string.Empty; // para invitado (cookie)
        public List<CartItem> Items { get; set; } = new();
        public bool IsActive { get; set; } = true;
        
        // Propiedades calculadas
        [NotMapped]
        public decimal Total 
        { 
            get 
            {
                return Items?.Where(i => i.IsActive).Sum(i => i.Subtotal) ?? 0;
            } 
        }
        
        [NotMapped]
        public int TotalItems 
        { 
            get 
            {
                return Items?.Where(i => i.IsActive).Sum(i => i.Quantity) ?? 0;
            } 
        }
    }

    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public Cart? Cart { get; set; }

        // ✅ CAMBIO: Ahora referencia directa a Product (no a ProductVariant)
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // ✅ NUEVO: Talla seleccionada
        [StringLength(10)]
        public string Size { get; set; } = string.Empty; // S, M, L, XL

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;

        // Propiedad calculada
        [NotMapped]
        public decimal Subtotal
        {
            get { return UnitPrice * Quantity; }
        }
    }
}