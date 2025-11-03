using System;
using System.ComponentModel.DataAnnotations;

namespace TiendaPlayeras.Web.Models
{
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public int ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Para que coincida con el índice en OnModelCreating
        public bool IsActive { get; set; } = true;

        // Navegación
        public ApplicationUser? User { get; set; }
        public Product? Product { get; set; }
    }
}
