using System.Collections.Generic;


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
}


public class CartItem
{
public int Id { get; set; }
public int CartId { get; set; }
public Cart? Cart { get; set; }
public int ProductVariantId { get; set; }
public ProductVariant? ProductVariant { get; set; }
public int Quantity { get; set; }
public decimal UnitPrice { get; set; }
public bool IsActive { get; set; } = true;
}


public class WishlistItem
{
public int Id { get; set; }
public string UserId { get; set; } = string.Empty;
public ApplicationUser? User { get; set; }
public int ProductVariantId { get; set; }
public ProductVariant? ProductVariant { get; set; }
public bool IsActive { get; set; } = true;
}
}