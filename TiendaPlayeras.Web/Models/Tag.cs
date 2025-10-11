using System.ComponentModel.DataAnnotations;

namespace TiendaPlayeras.Web.Models
{
    /// <summary>Etiqueta perteneciente a una categoría (Mustang, Ferrari, Rock...)</summary>
    public class Tag
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(140)]
        public string Slug { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<ProductTag> ProductTags { get; set; } = new();
    }
}
