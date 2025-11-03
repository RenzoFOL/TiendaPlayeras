using Microsoft.AspNetCore.Mvc;
using TiendaPlayeras.Web.Models;
using System;
using System.Collections.Generic;

namespace TiendaPlayeras.Web.Controllers
{
    public class PedidosController : Controller
    {
        // Helper para no duplicar los datos de ejemplo
        private static List<OrderDemoItem> GetDemoData() => new List<OrderDemoItem>
        {
            new OrderDemoItem { OrderNumber = "PP-000245", Date = DateTime.Today.AddDays(-2),  Status = "En proceso", Items = 3, Total = 699.00m },
            new OrderDemoItem { OrderNumber = "PP-000244", Date = DateTime.Today.AddDays(-10), Status = "Enviado",    Items = 2, Total = 499.00m },
            new OrderDemoItem { OrderNumber = "PP-000243", Date = DateTime.Today.AddDays(-20), Status = "Entregado",  Items = 1, Total = 299.00m },
        };

        [HttpGet]
        public IActionResult Index()
        {
            var demo = GetDemoData();
            return View(demo);
        }

        // NUEVO: devuelve la misma vista Index, pero como parcial (sin layout)
        [HttpGet]
        public IActionResult Partial()
        {
            var demo = GetDemoData();
            ViewBag.Partial = true;          // Para que Views/Pedidos/Index.cshtml no use Layout
            return View("Index", demo);      // Reutiliza la vista Index
        }
    }
}
