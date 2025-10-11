using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TiendaPlayeras.Web.Models;
namespace TiendaPlayeras.Web.Models
{
    /// <summary>Producto base (playera), sin especificar variante.</summary>

    public class Product
    {
        public List<ProductTag> ProductTags { get; set; } = new();
        public int Id { get; set; }
        public string? MainImagePath { get; set; }  // ej: /uploads/products/123/main.jpg


        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(140)]
        public string Slug { get; set; } = string.Empty; // para URL amigable

        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>Precio base del producto (sin variantes).</summary>
        [Range(0, 999999.99)]
        [Column(TypeName = "numeric(10,2)")]
        public decimal BasePrice { get; set; }

        /// <summary>Permite subir diseño propio.</summary>
        public bool IsCustomizable { get; set; } = false;

        /// <summary>Soft-delete: activo/inhabilitado (no se elimina físico).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<ProductVariant> Variants { get; set; } = new();

        // Opciones de variantes visibles en la página pública
    public bool UseFit { get; set; } = true;
    public bool UseColor { get; set; } = true;
    public bool UseSize { get; set; } = true;

    // Listas permitidas (CSV). Si están vacías, se infiere de las variantes creadas.
    [MaxLength(200)]
    public string? AllowedFitsCsv { get; set; }   // ej: "Hombre,Mujer,Unisex"
    [MaxLength(400)]
    public string? AllowedColorsCsv { get; set; } // ej: "Negro,Blanco,Rojo,#000000,#FFFFFF"
    [MaxLength(200)]
    public string? AllowedSizesCsv { get; set; }  // ej: "XS,S,M,L,XL,XXL"
    }

    /// <summary>Variante de producto: talla, corte, color, diseño.</summary>
    public class ProductVariant
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required, StringLength(20)]
        public string Size { get; set; } = string.Empty; // S, M, L, XL

        [Required, StringLength(20)]
        public string Fit { get; set; } = string.Empty;  // Hombre, Mujer, Unisex

        [Required, StringLength(30)]
        public string Color { get; set; } = string.Empty; // Negro, Blanco, etc.

        [StringLength(100)]
        public string DesignCode { get; set; } = string.Empty; // identificador de diseño

        /// <summary>Precio final de la variante (si la usas). Si prefieres usar BasePrice + modificadores, déjalo en 0 y calcula en la lógica.</summary>
        [Range(0, 999999.99)]
        [Column(TypeName = "numeric(10,2)")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool IsActive { get; set; } = true;
    }
}