using System.ComponentModel.DataAnnotations;

namespace TiendaPlayeras.Web.Models
{
    /// <summary>Categoría padre (Artistas, Coches, Deportes...)</summary>
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(140)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Soft delete.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<Tag> Tags { get; set; } = new();
    }
}
