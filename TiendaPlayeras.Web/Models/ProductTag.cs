namespace TiendaPlayeras.Web.Models
{
    /// <summary>Relación N:N Producto ↔ Tag</summary>
    public class ProductTag
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int TagId { get; set; }
        public Tag? Tag { get; set; }

        public bool IsActive { get; set; } = true; // soft delete de la relación
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
