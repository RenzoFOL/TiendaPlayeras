using System.Collections.Generic;

namespace TiendaPlayeras.Web.Models
{
    public class CheckoutViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
        public string Size { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }

        // 👇 NUEVO: indica si el checkout viene del carrito
        public bool FromCart { get; set; }

        // Id de la opción seleccionada (DeliveryOption.Id)
        public string SelectedDeliveryOptionId { get; set; } = string.Empty;

        // Siempre será "Contra Entrega", pero lo dejamos por claridad
        public string PaymentMethod { get; set; } = "Contra Entrega";

        public List<DeliveryOption> AvailableDeliveryOptions { get; set; } = new();
    }
}
