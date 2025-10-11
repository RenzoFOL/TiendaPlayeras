namespace TiendaPlayeras.Web.Models.ViewModels
{
    public class VariantsAdminViewModel
    {
        public Product Product { get; set; } = default!;
        public List<ProductVariant> Variants { get; set; } = new();
        
        // Para la UI (predefinidos)
        public string[] PredefinedFits { get; set; } = new[] { "Hombre", "Mujer", "Unisex", "Oversize", "Boxy" };
        public string[] PredefinedSizes { get; set; } = new[] { "XS", "S", "M", "L", "XL", "XXL", "Yasui" };
        public string[] PredefinedColors { get; set; } = new[] { "Negro", "Blanco", "Rojo", "Azul", "Verde", "Gris", "#000000", "#FFFFFF" };
    }
}