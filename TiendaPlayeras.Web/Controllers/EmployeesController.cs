using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles,
            ILogger<EmployeesController> logger)
        {
            _users = users;
            _roles = roles;
            _logger = logger;
        }

        // GET: /Employees
        public async Task<IActionResult> Index()
        {
            var list = await _users.GetUsersInRoleAsync("Employee");
            return View(list);
        }

        // GET: /Employees/Create
        public IActionResult Create() => View(new CreateEmployeeVm());

        // POST: /Employees/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var exists = await _users.FindByEmailAsync(vm.Email);
            if (exists != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "Ya existe un usuario con este correo.");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true,              // como lo crea un Admin
                PhoneNumber = vm.PhoneNumber
            };

            var pwd = string.IsNullOrWhiteSpace(vm.Password)
                ? "Empleado#123"                   // temporal (mejor genera y obliga cambio)
                : vm.Password!;

            var create = await _users.CreateAsync(user, pwd);
            if (!create.Succeeded)
            {
                foreach (var e in create.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            if (!await _roles.RoleExistsAsync("Employee"))
                await _roles.CreateAsync(new IdentityRole("Employee"));

            await _users.AddToRoleAsync(user, "Employee");

            TempData["Success"] = "Empleado creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Employees/ToggleLock/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Desbloquear
                user.LockoutEnd = null;
                await _users.UpdateAsync(user);
                TempData["Success"] = "Empleado desbloqueado.";
            }
            else
            {
                // Bloquear por 5 años
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(5);
                await _users.UpdateAsync(user);
                TempData["Success"] = "Empleado bloqueado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Employees/ResetPassword/{id}
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(new ResetEmployeePasswordVm { UserId = id });
        }

        // POST: /Employees/ResetPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetEmployeePasswordVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var user = await _users.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var res = await _users.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }
            TempData["Success"] = "Contraseña restablecida.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class CreateEmployeeVm
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        // opcional; si lo dejas vacío se usa un password temporal
        [System.ComponentModel.DataAnnotations.MinLength(6)]
        public string? Password { get; set; }
    }

    public class ResetEmployeePasswordVm
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string UserId { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}