using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using Microsoft.Extensions.Logging;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>
    /// Panel administrativo. Solo accesible por usuarios con rol Admin.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext db, ILogger<AdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>Vista principal del panel del Administrador.</summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // ✅ Obtener totales generales para el dashboard
                var totalProducts = await _db.Products.CountAsync();
                var activeProducts = await _db.Products.CountAsync(p => p.IsActive);

                ViewBag.TotalProducts = totalProducts;
                ViewBag.ActiveProducts = activeProducts;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar dashboard de productos");
                ViewBag.TotalProducts = 0;
                ViewBag.ActiveProducts = 0;
                return View();
            }
        }
    }
}