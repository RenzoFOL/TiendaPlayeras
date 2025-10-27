using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaPlayeras.Web.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // nulo si invitado
        public ApplicationUser? User { get; set; }
        public int? ShippingAddressId { get; set; }
        public UserAddress? ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Completed, Canceled
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; } = true;
        public List<OrderItem> Items { get; set; } = new();
        
        // Propiedad calculada para el número de orden
        [NotMapped]
        public string OrderNumber => $"ORD-{Id:D6}";

        public DateTime UpdatedAt { get; internal set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        // ✅ ACTUALIZADO: Referencia directa a Product (no a ProductVariant)
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        // ✅ NUEVO: Talla seleccionada
        [Column(TypeName = "varchar(10)")]
        public string Size { get; set; } = string.Empty; // S, M, L, XL
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Propiedad calculada
        [NotMapped]
        public decimal Subtotal => UnitPrice * Quantity;
    }

    // Clase demo opcional para listados/ejemplos (no es entidad EF)
    public class OrderDemoItem
    {
        public string   OrderNumber { get; set; } = "";
        public DateTime Date        { get; set; }
        public string   Status      { get; set; } = "";
        public int      Items       { get; set; }
        public decimal  Total       { get; set; }
    }
}