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
        
        // ✅ NUEVOS CAMPOS BOOLEANOS PARA TALLAS
        public bool SizeS { get; set; } = true;
        public bool SizeM { get; set; } = true;
        public bool SizeL { get; set; } = true;
        public bool SizeXL { get; set; } = true;
        
        // 🔄 MANTENER para compatibilidad temporal
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

        // ✅ ACTUALIZADO: AvailableSizesList ahora usa los booleanos
        [NotMapped]
        public List<string> AvailableSizesList 
        { 
            get 
            {
                var sizes = new List<string>();
                if (SizeS) sizes.Add("S");
                if (SizeM) sizes.Add("M");
                if (SizeL) sizes.Add("L");
                if (SizeXL) sizes.Add("XL");
                return sizes;
            }
            set 
            {
                // Actualizar booleanos según la lista
                SizeS = value?.Contains("S") ?? true;
                SizeM = value?.Contains("M") ?? true;
                SizeL = value?.Contains("L") ?? true;
                SizeXL = value?.Contains("XL") ?? true;
                
                // Mantener AvailableSizes actualizado
                UpdateAvailableSizes();
            }
        }

        // ✅ NUEVO: Método para mantener AvailableSizes sincronizado
        public void UpdateAvailableSizes()
        {
            AvailableSizes = string.Join(",", AvailableSizesList);
        }
    }
}