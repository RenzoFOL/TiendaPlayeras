using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
        
        // 👇 NUEVO: ID del usuario para filtrar sus pedidos
        public string? UserId { get; set; }

        public string DeliveryPoint { get; set; } = string.Empty;
        public string DeliverySchedule { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // 👇 NUEVO: Estado actual del pedido
        public string Status { get; set; } = "Pending";

        // 👇 NUEVO: Historial de estados en JSON
        public string StatusHistory { get; set; } = "[]";

        // Nombre del archivo PDF guardado en wwwroot/tickets
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
}