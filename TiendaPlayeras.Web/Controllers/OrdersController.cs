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
    // ... código de validación existente ...

    var selectedOption = DeliveryOptions.All
        .FirstOrDefault(o => o.Id == model.SelectedDeliveryOptionId);

    if (selectedOption == null)
    {
        ModelState.AddModelError("SelectedDeliveryOptionId", "Opción de entrega no válida.");
        return View(model);
    }

    // 👇 NUEVO: Variable para redirección
    int orderId = 0;

    if (model.FromCart)
    {
        // ---------- FLUJO CARRITO (MÚLTIPLES PRODUCTOS) ----------
        if (string.IsNullOrEmpty(CurrentUserId))
            return Unauthorized();

        var summary = await _cart.GetSummaryAsync(CurrentUserId);
        if (summary == null || summary.Lines == null || !summary.Lines.Any())
        {
            ModelState.AddModelError(string.Empty, "Tu carrito está vacío.");
            return View(model);
        }

        // 👇 CREAR ORDER COMPLETA
        var order = new Order
        {
            UserId = CurrentUserId,
            UserName = User?.Identity?.Name ?? "Invitado",
            Status = "Pending",
            Subtotal = summary.TotalAmount,
            Shipping = 0, // Envío gratis por ahora
            Total = summary.TotalAmount,
            DeliveryPoint = selectedOption.Point,
            DeliverySchedule = selectedOption.Schedule,
            PaymentMethod = "Contra Entrega",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Agregar items del carrito
        foreach (var cartItem in summary.Lines)
        {
            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                Size = cartItem.Size,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice
            };
            order.Items.Add(orderItem);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Generar número de orden
        order.GenerateOrderNumber();
        await _context.SaveChangesAsync();

        orderId = order.Id;

        // 👇 CREAR TICKET SIMPLIFICADO para el carrito (opcional)
        var ticket = new OrderTicket
        {
            ProductId = 0,
            ProductName = $"Orden #{order.OrderNumber} - {summary.TotalItems} productos",
            UnitPrice = summary.TotalAmount,
            Quantity = 1,
            Size = "VARIOS",
            TotalPrice = summary.TotalAmount,
            UserName = User?.Identity?.Name ?? "Invitado",
            UserId = CurrentUserId,
            DeliveryPoint = selectedOption.Point,
            DeliverySchedule = selectedOption.Schedule,
            PaymentMethod = "Contra Entrega",
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };
        ticket.AddStatusHistory("Pending", "System", "Pedido creado automáticamente");

        _context.OrderTickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Generar PDF del ticket
        var ticketsFolder = Path.Combine(_env.WebRootPath, "tickets");
        if (!Directory.Exists(ticketsFolder))
            Directory.CreateDirectory(ticketsFolder);

        var pdfFileName = $"ticket-{ticket.Id}.pdf";
        var pdfFilePath = Path.Combine(ticketsFolder, pdfFileName);

        var document = new TicketDocument(ticket);
        document.GeneratePdf(pdfFilePath);

        ticket.PdfFileName = pdfFileName;
        await _context.SaveChangesAsync();

        // Limpiar carrito
        await _cart.ClearAsync(CurrentUserId);
    }
    else
    {
        // ---------- FLUJO PRODUCTO INDIVIDUAL (EXISTENTE) ----------
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == model.ProductId);

        if (product == null)
            return NotFound();

        if (model.Quantity < 1)
            model.Quantity = 1;

        var totalPrice = product.BasePrice * model.Quantity;
        model.TotalPrice = totalPrice;

        // Crear Order para producto individual también
        var order = new Order
        {
            UserId = CurrentUserId,
            UserName = User?.Identity?.Name ?? "Invitado",
            Status = "Pending",
            Subtotal = totalPrice,
            Shipping = 0,
            Total = totalPrice,
            DeliveryPoint = selectedOption.Point,
            DeliverySchedule = selectedOption.Schedule,
            PaymentMethod = "Contra Entrega",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Agregar item individual
        var orderItem = new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Size = model.Size ?? string.Empty,
            Quantity = model.Quantity,
            UnitPrice = product.BasePrice
        };
        order.Items.Add(orderItem);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        order.GenerateOrderNumber();
        await _context.SaveChangesAsync();

        orderId = order.Id;

        // Ticket existente (compatibilidad)
        var ticket = new OrderTicket
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.BasePrice,
            Quantity = model.Quantity,
            Size = model.Size ?? string.Empty,
            TotalPrice = totalPrice,
            UserName = User?.Identity?.Name ?? "Invitado",
            UserId = CurrentUserId,
            DeliveryPoint = selectedOption.Point,
            DeliverySchedule = selectedOption.Schedule,
            PaymentMethod = "Contra Entrega",
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };
        ticket.AddStatusHistory("Pending", "System", "Pedido creado automáticamente");

        _context.OrderTickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Generar PDF
        var ticketsFolder = Path.Combine(_env.WebRootPath, "tickets");
        if (!Directory.Exists(ticketsFolder))
            Directory.CreateDirectory(ticketsFolder);

        var pdfFileName = $"ticket-{ticket.Id}.pdf";
        var pdfFilePath = Path.Combine(ticketsFolder, pdfFileName);

        var document = new TicketDocument(ticket);
        document.GeneratePdf(pdfFilePath);

        ticket.PdfFileName = pdfFileName;
        await _context.SaveChangesAsync();
    }

    // Redirigir a confirmación de ORDER (no de ticket)
    return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
}

// 👇 NUEVO: Método de confirmación para Orders
[HttpGet]
public async Task<IActionResult> OrderConfirmation(int id)
{
    var order = await _context.Orders
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order == null)
        return NotFound();

    // Crear ViewModel para la confirmación
    var viewModel = new OrderConfirmationViewModel
    {
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        Total = order.Total,
        DeliveryPoint = order.DeliveryPoint,
        DeliverySchedule = order.DeliverySchedule,
        PaymentMethod = order.PaymentMethod,
        UserName = order.UserName,
        Items = order.Items.Select(i => new OrderItemViewModel
        {
            ProductName = i.ProductName,
            Size = i.Size,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList()
    };

    return View(viewModel);
}


    }
}
