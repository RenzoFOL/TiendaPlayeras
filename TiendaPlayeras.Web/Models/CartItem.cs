using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaPlayeras.Web.Models
{
    public class CartItem
    {
        [Key] public int Id { get; set; }

        [Required] public string UserId { get; set; } = string.Empty;

        [Required] public int ProductId { get; set; }

        // Guarda el precio en el momento de agregar al carrito (snapshot)
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required, StringLength(10)]
        public string Size { get; set; } = "M";

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        // Opcional para mostrar imagen/nombre sin JOINs pesados (puedes omitir)
        public string? ProductName { get; set; }
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
