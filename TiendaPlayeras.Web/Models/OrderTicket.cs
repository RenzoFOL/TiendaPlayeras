using System;

namespace TiendaPlayeras.Web.Models
{
    public class OrderTicket
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
        public string Size { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string DeliveryPoint { get; set; } = string.Empty;
        public string DeliverySchedule { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty; // "Contra Entrega"

        public DateTime CreatedAt { get; set; }

        // Nombre del archivo PDF guardado en wwwroot/tickets
        public string PdfFileName { get; set; } = string.Empty;
    }
}
