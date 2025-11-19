using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Services;
using QuestPDF.Fluent;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize] // si quieres que solo usuarios logueados compren, si no, quita este atributo
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICartService _cart;

        // Igual que en CartController, pero usando Claims
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public OrdersController(ApplicationDbContext context, IWebHostEnvironment env, ICartService cart)
        {
            _context = context;
            _env = env;
            _cart = cart;
        }

        // POST: /Orders/CheckoutFromProduct
        // Llega desde el botón "COMPRAR AHORA" en la vista de producto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutFromProduct(int productId, int quantity, string size)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            if (quantity < 1)
                quantity = 1;

            var totalPrice = product.BasePrice * quantity;

            var model = new CheckoutViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.BasePrice,
                Quantity = quantity,
                Size = size ?? string.Empty,
                TotalPrice = totalPrice,
                AvailableDeliveryOptions = DeliveryOptions.All,
                FromCart = false
            };

            if (model.AvailableDeliveryOptions.Any())
                model.SelectedDeliveryOptionId = model.AvailableDeliveryOptions.First().Id;

            return View("Checkout", model);
        }

        // GET: /Orders/CheckoutFromCart
        // Llega desde el botón "COMPRAR" del carrito
        [HttpGet]
        public async Task<IActionResult> CheckoutFromCart()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                // Por seguridad, mandamos a login si no hay usuario
                return RedirectToAction("Login", "Account");
            }

            var summary = await _cart.GetSummaryAsync(CurrentUserId);
            if (summary == null || summary.Lines == null || !summary.Lines.Any())
            {
                // Si el carrito está vacío, regresar a la vista del carrito
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                FromCart = true,
                ProductId = 0, // No estamos comprando un solo producto
                ProductName = "Productos en tu carrito",
                UnitPrice = summary.TotalAmount,
                Quantity = summary.TotalItems,
                Size = "Varios",
                TotalPrice = summary.TotalAmount,
                AvailableDeliveryOptions = DeliveryOptions.All
            };

            if (model.AvailableDeliveryOptions.Any())
                model.SelectedDeliveryOptionId = model.AvailableDeliveryOptions.First().Id;

            // Reusamos la MISMA vista Checkout
            return View("Checkout", model);
        }

        // GET: /Orders/Checkout?productId=5
        // (opcional, por si alguien llega directo por URL)
        [HttpGet]
        public async Task<IActionResult> Checkout(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var model = new CheckoutViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.BasePrice,
                Quantity = 1,
                Size = string.Empty,
                TotalPrice = product.BasePrice,
                AvailableDeliveryOptions = DeliveryOptions.All,
                FromCart = false
            };

            // Por defecto, seleccionamos la primera opción
            if (model.AvailableDeliveryOptions.Any())
                model.SelectedDeliveryOptionId = model.AvailableDeliveryOptions.First().Id;

            return View(model);
        }

        // POST: /Orders/Checkout
        // POST: /Orders/Checkout
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Checkout(CheckoutViewModel model)
{
    // Recargamos opciones en el modelo para re-mostrar la vista si hay error
    model.AvailableDeliveryOptions = DeliveryOptions.All;

    if (string.IsNullOrEmpty(model.SelectedDeliveryOptionId))
    {
        ModelState.AddModelError("SelectedDeliveryOptionId", "Selecciona un lugar y fecha de entrega.");
    }

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var selectedOption = DeliveryOptions.All
        .FirstOrDefault(o => o.Id == model.SelectedDeliveryOptionId);

    if (selectedOption == null)
    {
        ModelState.AddModelError("SelectedDeliveryOptionId", "Opción de entrega no válida.");
        return View(model);
    }

    OrderTicket ticket;

    if (model.FromCart)
    {
        // ---------- Flujo: compra desde el carrito ----------
        if (string.IsNullOrEmpty(CurrentUserId))
            return Unauthorized();

        var summary = await _cart.GetSummaryAsync(CurrentUserId);
        if (summary == null || summary.Lines == null || !summary.Lines.Any())
        {
            ModelState.AddModelError(string.Empty, "Tu carrito está vacío.");
            return View(model);
        }

        var totalPrice = summary.TotalAmount;
        var totalItems = summary.TotalItems;

        // Actualizamos el modelo para coherencia
        model.Quantity = totalItems;
        model.TotalPrice = totalPrice;
        model.ProductName = $"Compra de carrito ({totalItems} productos)";
        model.Size = "Varios";

        // Creamos un solo ticket que representa la compra completa del carrito
        ticket = new OrderTicket
        {
            ProductId = 0, // No hay un solo producto, es el carrito
            ProductName = model.ProductName,
            UnitPrice = totalPrice,  // mostramos el total como un ítem
            Quantity = 1,
            Size = "VARIOS",
            TotalPrice = totalPrice,
            UserName = User?.Identity?.Name ?? "Invitado",
            UserId = CurrentUserId, // 👈 AQUÍ
            DeliveryPoint = selectedOption.Point,
            DeliverySchedule = selectedOption.Schedule,
            PaymentMethod = "Contra Entrega",
            CreatedAt = DateTime.UtcNow
        };

        // Agregar historial inicial
        ticket.AddStatusHistory(OrderStatus.Pending, "System", "Pedido creado automáticamente");

        // Limpiamos el carrito después de generar el ticket
        await _cart.ClearAsync(CurrentUserId);
    }
    else
    {
        // ---------- Flujo original: compra de un solo producto ----------
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == model.ProductId);

        if (product == null)
            return NotFound();

        // Normalizar cantidad y recalcular total
        if (model.Quantity < 1)
            model.Quantity = 1;

        var totalPrice = product.BasePrice * model.Quantity;
        model.TotalPrice = totalPrice;

        ticket = new OrderTicket
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.BasePrice,
            Quantity = model.Quantity,
            Size = model.Size ?? string.Empty,
            TotalPrice = totalPrice,
            UserName = User?.Identity?.Name ?? "Invitado",
            UserId = CurrentUserId, // 👈 AQUÍ
            DeliveryPoint = selectedOption.Point,
            DeliverySchedule = selectedOption.Schedule,
            PaymentMethod = "Contra Entrega",
            CreatedAt = DateTime.UtcNow
        };

        // Agregar historial inicial (SOLO UNA VEZ)
        ticket.AddStatusHistory(OrderStatus.Pending, "System", "Pedido creado automáticamente");
    }

    // Guardar el ticket en la base de datos
    _context.OrderTickets.Add(ticket);
    await _context.SaveChangesAsync();

    // Generar y guardar PDF en wwwroot/tickets
    var ticketsFolder = Path.Combine(_env.WebRootPath, "tickets");
    if (!Directory.Exists(ticketsFolder))
        Directory.CreateDirectory(ticketsFolder);

    var pdfFileName = $"ticket-{ticket.Id}.pdf";
    var pdfFilePath = Path.Combine(ticketsFolder, pdfFileName);

    var document = new TicketDocument(ticket);
    document.GeneratePdf(pdfFilePath);

    // Actualizar el ticket con el nombre del archivo PDF
    ticket.PdfFileName = pdfFileName;
    await _context.SaveChangesAsync();

    // Redirigir a página de confirmación
    return RedirectToAction(nameof(Confirmation), new { id = ticket.Id });
}

        // GET: /Orders/Confirmation/10
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var ticket = await _context.OrderTickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            var model = new OrderConfirmationViewModel
            {
                TicketId = ticket.Id,
                ProductName = ticket.ProductName,
                UnitPrice = ticket.UnitPrice,
                Quantity = ticket.Quantity,
                Size = ticket.Size,
                TotalPrice = ticket.TotalPrice,
                UserName = ticket.UserName,
                DeliveryPoint = ticket.DeliveryPoint,
                DeliverySchedule = ticket.DeliverySchedule,
                PaymentMethod = ticket.PaymentMethod,
                CreatedAt = ticket.CreatedAt,
                PdfFileName = ticket.PdfFileName
            };

            return View(model);
        }
    }
}
