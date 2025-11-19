using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminOrders
        public async Task<IActionResult> Index(string status = "", string search = "")
{
    var query = _context.Orders
        .Include(o => o.Items)
        .Include(o => o.User)
        .AsQueryable();

    if (!string.IsNullOrEmpty(status))
    {
        query = query.Where(o => o.Status == status);
    }

    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(o => 
            o.OrderNumber.Contains(search) ||
            o.User!.UserName!.Contains(search) ||
            o.Items.Any(i => i.ProductName.Contains(search)));
    }

    var orders = await query
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    ViewBag.StatusFilter = status;
    ViewBag.SearchFilter = search;
    ViewBag.StatusOptions = OrderStatus.GetAllStatuses();

    return View(orders);
}

        // GET: /AdminOrders/Details/5
       public async Task<IActionResult> Details(int id)
{
    var order = await _context.Orders
        .Include(o => o.Items)
        .Include(o => o.User)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order == null)
    {
        return NotFound();
    }

    return View(order);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateStatus(int id, string newStatus, string? notes = null)
{
    var order = await _context.Orders.FindAsync(id);
    if (order == null)
    {
        return NotFound();
    }

    var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "Administrador";
    
    // Actualizar estado
    order.Status = newStatus;
    order.UpdatedAt = DateTime.UtcNow;
    
    // Aquí podrías agregar un sistema de historial para Orders si lo necesitas
    // order.AddStatusHistory(newStatus, currentUser, notes);

    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = $"Estado actualizado a {OrderStatus.GetDisplayName(newStatus)}";

    return RedirectToAction(nameof(Details), new { id });
}
        // GET: /AdminOrders/GetStatusHistory/5
        public async Task<IActionResult> GetStatusHistory(int id)
        {
            var order = await _context.OrderTickets.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var history = order.GetStatusHistory()
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            return Json(history);
        }
    }
}