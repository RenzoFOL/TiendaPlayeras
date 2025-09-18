using System;
using System.Collections.Generic;


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
}


public class OrderItem
{
public int Id { get; set; }
public int OrderId { get; set; }
public Order? Order { get; set; }
public int ProductVariantId { get; set; }
public ProductVariant? ProductVariant { get; set; }
public int Quantity { get; set; }
public decimal UnitPrice { get; set; }
public bool IsActive { get; set; } = true;
}
}