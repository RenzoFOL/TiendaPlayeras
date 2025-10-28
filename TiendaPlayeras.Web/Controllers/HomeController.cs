using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // “Novedades”: últimos activos
                var newArrivals = await _db.Products
                    .Include(p => p.ProductImages)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // “Best sellers”: productos con etiqueta "best" o más populares
                var bestSellers = await _db.Products
                    .Include(p => p.ProductImages)
                    .Where(p => p.IsActive && 
                        p.ProductTags.Any(pt => pt.IsActive && pt.Tag != null && pt.Tag.Slug == "best"))
                    .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // Categorías destacadas con imagen representativa
                var categories = await _db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Take(3)
                    .ToListAsync();

                // Para cada categoría, obtener una imagen representativa (primera playera de esa categoría)
                foreach (var category in categories)
                {
                    var representativeProduct = await _db.Products
                        .Include(p => p.ProductImages)
                        .Where(p => p.IsActive && 
                            p.ProductTags.Any(pt => 
                                pt.IsActive && 
                                pt.Tag != null && 
                                pt.Tag.CategoryId == category.Id))
                        .OrderBy(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (representativeProduct?.ProductImages?.FirstOrDefault() != null)
                    {
                        // Guardar la imagen representativa en ViewBag temporal
                        ViewData[$"CategoryImage_{category.Id}"] = representativeProduct.ProductImages.First().Path;
                    }
                    else
                    {
                        // Imagen por defecto si no hay productos
                        ViewData[$"CategoryImage_{category.Id}"] = "/images/cat-item1.jpg";
                    }
                }

                ViewBag.NewArrivals = newArrivals;
                ViewBag.BestSellers = bestSellers;
                ViewBag.Categories = categories;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la página de inicio");
                // En caso de error, retornar listas vacías
                ViewBag.NewArrivals = new List<Product>();
                ViewBag.BestSellers = new List<Product>();
                ViewBag.Categories = new List<Category>();
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}