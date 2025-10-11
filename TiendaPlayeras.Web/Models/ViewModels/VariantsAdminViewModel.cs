namespace TiendaPlayeras.Web.Models.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de administración de variantes de un producto
    /// </summary>
    public class VariantsAdminViewModel
    {
        /// <summary>
        /// Producto principal del cual se gestionan las variantes
        /// </summary>
        public Product Product { get; set; } = default!;
        
        /// <summary>
        /// Lista de variantes existentes para este producto
        /// </summary>
        public List<ProductVariant> Variants { get; set; } = new();
        
        /// <summary>
        /// Opciones predefinidas de Fits para mostrar en la UI
        /// </summary>
        public string[] PredefinedFits { get; set; } = new[] 
        { 
            "Hombre", "Mujer", "Unisex", "Oversize", "Boxy" 
        };
        
        /// <summary>
        /// Opciones predefinidas de Tallas para mostrar en la UI
        /// </summary>
        public string[] PredefinedSizes { get; set; } = new[] 
        { 
            "XS", "S", "M", "L", "XL", "XXL", "Yasui" 
        };
        
        /// <summary>
        /// Opciones predefinidas de Colores para mostrar en la UI
        /// </summary>
        public string[] PredefinedColors { get; set; } = new[] 
        { 
            "Negro", "Blanco", "Rojo", "Azul", "Verde", "Gris", "Amarillo", "Rosa" 
        };

        /// <summary>
        /// Indica si hay variantes activas para este producto
        /// </summary>
        public bool HasActiveVariants => Variants.Any(v => v.IsActive);

        /// <summary>
        /// Cuenta total de variantes activas
        /// </summary>
        public int ActiveVariantsCount => Variants.Count(v => v.IsActive);

        /// <summary>
        /// Cuenta total de variantes (activas e inactivas)
        /// </summary>
        public int TotalVariantsCount => Variants.Count;
    }
}