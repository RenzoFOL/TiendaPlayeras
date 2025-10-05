using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace TiendaPlayeras.Web.Controllers
{
/// <summary>
/// Panel administrativo. Solo accesible por usuarios con rol Admin.
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
/// <summary>Vista principal del panel del Administrador.</summary>
public IActionResult Index() => View();
}
}