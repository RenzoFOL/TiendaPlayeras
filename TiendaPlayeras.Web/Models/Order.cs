using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TiendaPlayeras.Web.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        public string? UserId { get; set; } // nulo si invitado
        public ApplicationUser? User { get; set; }
        
        // Información del cliente (para mostrar sin necesidad de join)
        public string UserName { get; set; } = string.Empty;
        
        public int? ShippingAddressId { get; set; }
        public UserAddress? ShippingAddress { get; set; }
        
        // Información de entrega específica
        public string DeliveryPoint { get; set; } = string.Empty;
        public string DeliverySchedule { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Contra Entrega";
        
        // Número de orden único
        public string OrderNumber { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, InProgress, ReadyForPickup, Completed, Cancelled
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; } = true;
        
        public List<OrderItem> Items { get; set; } = new();

        // Método para generar número de orden
        public void GenerateOrderNumber()
        {
            if (string.IsNullOrEmpty(OrderNumber))
            {
                OrderNumber = $"ORD-{CreatedAt:yyyyMMdd}-{Id:D5}";
            }
        }

        // Propiedades calculadas útiles
        [NotMapped]
        public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;
        
        [NotMapped]
        public string StatusDisplay => OrderStatus.GetDisplayName(Status);
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        // Referencia directa a Product
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        // Nombre del producto (para mostrar sin join)
        public string ProductName { get; set; } = string.Empty;
        
        // Talla seleccionada
        [Column(TypeName = "varchar(10)")]
        public string Size { get; set; } = string.Empty; // S, M, L, XL
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Propiedad calculada para subtotal
        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;

        // Propiedad calculada para display
        [NotMapped]
        public string DisplaySubtotal => TotalPrice.ToString("C");
    }

    // Clase para manejar estados de orden
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string InProgress = "InProgress";
        public const string ReadyForPickup = "ReadyForPickup";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly Dictionary<string, string> DisplayNames = new()
        {
            { Pending, "Pendiente" },
            { Confirmed, "Confirmado" },
            { InProgress, "En Progreso" },
            { ReadyForPickup, "Listo para Recoger" },
            { Completed, "Completado" },
            { Cancelled, "Cancelado" }
        };

        public static string GetDisplayName(string status)
        {
            return DisplayNames.ContainsKey(status) ? DisplayNames[status] : status;
        }

        public static List<string> GetAllStatuses()
        {
            return new List<string> { Pending, Confirmed, InProgress, ReadyForPickup, Completed, Cancelled };
        }
    }

    // ViewModel para confirmación de orden
    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay => OrderStatus.GetDisplayName(Status);
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public string DeliveryPoint { get; set; } = string.Empty;
        public string DeliverySchedule { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public List<OrderItemViewModel> Items { get; set; } = new();
        public string UserName { get; set; } = string.Empty;
        
        // Para compatibilidad con el sistema existente
        public string PdfFileName { get; set; } = string.Empty;
    }

    // ViewModel para items de orden
    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string DisplayUnitPrice => UnitPrice.ToString("C");
        public string DisplayTotalPrice => TotalPrice.ToString("C");
    }

    // Sistema de tickets (compatibilidad con sistema existente)
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
        public string? UserId { get; set; }
        public string DeliveryPoint { get; set; } = string.Empty;
        public string DeliverySchedule { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Contra Entrega";
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public string StatusHistory { get; set; } = "[]";
        public string PdfFileName { get; set; } = string.Empty;

        // Métodos para manejar el historial
        public List<StatusHistoryEntry> GetStatusHistory()
        {
            try
            {
                return JsonSerializer.Deserialize<List<StatusHistoryEntry>>(StatusHistory) 
                    ?? new List<StatusHistoryEntry>();
            }
            catch
            {
                return new List<StatusHistoryEntry>();
            }
        }

        public void AddStatusHistory(string status, string changedBy, string? notes = null)
        {
            var history = GetStatusHistory();
            history.Add(new StatusHistoryEntry
            {
                Status = status,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Notes = notes
            });
            StatusHistory = JsonSerializer.Serialize(history);
        }
    }

    public class StatusHistoryEntry
    {
        public string Status { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    // Clase demo opcional para listados/ejemplos (no es entidad EF)
    public class OrderDemoItem
    {
        public string OrderNumber { get; set; } = "";
        public DateTime Date { get; set; }
        public string Status { get; set; } = "";
        public string StatusDisplay => OrderStatus.GetDisplayName(Status);
        public int Items { get; set; }
        public decimal Total { get; set; }
        public string CustomerName { get; set; } = "";
    }
}