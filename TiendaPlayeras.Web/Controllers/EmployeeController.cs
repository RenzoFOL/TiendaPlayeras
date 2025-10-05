using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>Panel del Empleado.</summary>
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        public IActionResult Index() => View();
    }
}