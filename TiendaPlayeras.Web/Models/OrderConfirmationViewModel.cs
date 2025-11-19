using System;

namespace TiendaPlayeras.Web.Models
{
    public class OrderConfirmationViewModel
    {
        public int TicketId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
        public string Size { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string DeliveryPoint { get; set; } = string.Empty;
        public string DeliverySchedule { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string PdfFileName { get; set; } = string.Empty;
    }
}
