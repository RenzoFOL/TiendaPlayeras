using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaPlayeras.Web.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(140)]
        public string Slug { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [Range(0, 999999.99)]
        [Column(TypeName = "numeric(10,2)")]
        public decimal BasePrice { get; set; }
        
        // Nueva columna para tallas disponibles (S,M,L,XL)
        [StringLength(50)]
        public string AvailableSizes { get; set; } = "S,M,L,XL";
        
        public bool IsCustomizable { get; set; } = false;
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Relaciones
        public List<ProductTag> ProductTags { get; set; } = new();
        public List<ProductImage> ProductImages { get; set; } = new();
        
        // Propiedad de conveniencia para obtener la imagen principal
        [NotMapped]
        public string? MainImagePath 
        { 
            get 
            {
                return ProductImages?
                    .OrderBy(x => x.DisplayOrder)
                    .FirstOrDefault(x => x.IsMain)?
                    .Path ?? ProductImages?
                    .OrderBy(x => x.DisplayOrder)
                    .FirstOrDefault()?
                    .Path;
            } 
        }

        // Propiedad de conveniencia para obtener tallas como lista
        [NotMapped]
        public List<string> AvailableSizesList 
        { 
            get 
            {
                return AvailableSizes?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList() ?? new List<string> { "S", "M", "L", "XL" };
            }
            set 
            {
                AvailableSizes = string.Join(",", value ?? new List<string> { "S", "M", "L", "XL" });
            }
        }
    }
}