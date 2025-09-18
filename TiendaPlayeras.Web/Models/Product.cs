using System.Collections.Generic;


namespace TiendaPlayeras.Web.Models
{
/// <summary>Producto base (playera), sin especificar variante.</summary>
public class Product
{
public int Id { get; set; }
public string Name { get; set; } = string.Empty;
public string Slug { get; set; } = string.Empty; // para URL amigable
public string? Description { get; set; }
public bool IsCustomizable { get; set; } = false; // permite subir diseño propio
public bool IsActive { get; set; } = true;
public List<ProductVariant> Variants { get; set; } = new();
}


/// <summary>Variante de producto: talla, corte, color, diseño.</summary>
public class ProductVariant
{
public int Id { get; set; }
public int ProductId { get; set; }
public Product? Product { get; set; }
public string Size { get; set; } = string.Empty; // S, M, L, XL
public string Fit { get; set; } = string.Empty; // Hombre, Mujer, Unisex
public string Color { get; set; } = string.Empty; // Negro, Blanco, etc.
public string DesignCode { get; set; } = string.Empty; // identificador de diseño
public decimal Price { get; set; }
public int Stock { get; set; }
public bool IsActive { get; set; } = true;
}
}