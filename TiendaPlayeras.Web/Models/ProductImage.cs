using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaPlayeras.Web.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        
        public Product? Product { get; set; }

        [Required, StringLength(500)]
        public string Path { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsMain { get; set; } = false;

        [StringLength(200)]
        public string? AltText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}