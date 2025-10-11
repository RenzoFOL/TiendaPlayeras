using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Data;

namespace TiendaPlayeras.Web.Controllers;

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
        // “Novedades”: últimos activos
        var newArrivals = await _db.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync();

        // “Best sellers”: por ahora usa etiqueta “best” si existe (luego lo ligamos a ventas reales)
        var bestTag = "best";
        var bestSellers = await _db.Products
            .Where(p => p.IsActive && p.ProductTags.Any(pt => pt.IsActive && pt.Tag!.Slug == bestTag))
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Take(10)
            .ToListAsync();

        // 3 categorías destacadas (elige las que tengas)
        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Take(3)
            .ToListAsync();

        ViewBag.NewArrivals = newArrivals;
        ViewBag.BestSellers = bestSellers;
        ViewBag.Categories = categories;
        return View();
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