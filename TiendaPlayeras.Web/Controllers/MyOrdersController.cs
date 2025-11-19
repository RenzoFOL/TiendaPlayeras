using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using System.IO;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize]
    public class MyOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public MyOrdersController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /MyOrders
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == CurrentUserId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: /MyOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Cambiar para buscar en Orders en lugar de OrderTickets
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == CurrentUserId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: /MyOrders/Partial
        [HttpGet]
        public async Task<IActionResult> Partial()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Unauthorized();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == CurrentUserId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return PartialView("_OrdersPartial", orders);
        }

        // GET: /MyOrders/DownloadTicket/5
        public async Task<IActionResult> DownloadTicket(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Unauthorized();
            }

            // Buscar en OrderTickets para compatibilidad
            var orderTicket = await _context.OrderTickets
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == CurrentUserId);

            if (orderTicket == null || string.IsNullOrEmpty(orderTicket.PdfFileName))
            {
                return NotFound();
            }

            var filePath = Path.Combine(_env.WebRootPath, "tickets", orderTicket.PdfFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/pdf", $"ticket-{orderTicket.Id}.pdf");
        }
    }
}