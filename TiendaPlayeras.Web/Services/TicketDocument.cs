using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TiendaPlayeras.Web.Models;
using System;

namespace TiendaPlayeras.Web.Services
{
    public class TicketDocument : IDocument
    {
        private readonly OrderTicket _ticket;

        public TicketDocument(OrderTicket ticket)
        {
            _ticket = ticket;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Content()
                    .Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("TiendaPlayeras").FontSize(20).SemiBold();
                        col.Item().Text("Ticket de pedido").FontSize(16);

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Folio: {_ticket.Id}");
                        col.Item().Text($"Fecha: {_ticket.CreatedAt:dd/MM/yyyy HH:mm}");

                        if (!string.IsNullOrEmpty(_ticket.UserName))
                            col.Item().Text($"Cliente: {_ticket.UserName}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Producto: {_ticket.ProductName}");
                        col.Item().Text($"Talla: {_ticket.Size}");
                        col.Item().Text($"Cantidad: {_ticket.Quantity}");
                        col.Item().Text($"Precio unitario: {_ticket.UnitPrice:C}");
                        col.Item().Text($"Total: {_ticket.TotalPrice:C}");

                        col.Item().Text($"Punto de entrega: {_ticket.DeliveryPoint}");
                        col.Item().Text($"Horario: {_ticket.DeliverySchedule}");

                        col.Item().Text($"Método de pago: {_ticket.PaymentMethod}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text("Indicaciones:")
                            .SemiBold();

                        col.Item().Text(text =>
                        {
                            text.Span("Presenta este ticket al momento de recoger tu pedido en el punto de entrega seleccionado. ");
                            text.Span("Si no puedes asistir en el horario indicado, comunícate con la tienda.");
                        });
                    });
            });
        }
    }
}
