using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Models.Employees;

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

            var vm = new EmployeesIndexVm
            {
                Employees = list,
                CreateModel = new CreateEmployeeVm(),
                ResetModel = new ResetEmployeePasswordVm()
            };

            return View(vm);
        }

        // POST: /Employees/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateModel")] CreateEmployeeVm model)
        {
            if (!ModelState.IsValid)
            {
                var list = await _users.GetUsersInRoleAsync("Employee");
                var vm = new EmployeesIndexVm
                {
                    Employees = list,
                    CreateModel = model,
                    ResetModel = new ResetEmployeePasswordVm()
                };
                return View("Index", vm);
            }

            var exists = await _users.FindByEmailAsync(model.Email);
            if (exists != null)
            {
                ModelState.AddModelError("CreateModel.Email", "Ya existe un usuario con este correo.");
                var list = await _users.GetUsersInRoleAsync("Employee");
                var vm = new EmployeesIndexVm
                {
                    Employees = list,
                    CreateModel = model,
                    ResetModel = new ResetEmployeePasswordVm()
                };
                return View("Index", vm);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                PhoneNumber = model.PhoneNumber
            };

            var pwd = string.IsNullOrWhiteSpace(model.Password) ? "Empleado#123" : model.Password!;
            var create = await _users.CreateAsync(user, pwd);

            if (!create.Succeeded)
            {
                foreach (var e in create.Errors)
                    ModelState.AddModelError("CreateModel.Password", e.Description);

                var list = await _users.GetUsersInRoleAsync("Employee");
                var vm = new EmployeesIndexVm
                {
                    Employees = list,
                    CreateModel = model,
                    ResetModel = new ResetEmployeePasswordVm()
                };
                return View("Index", vm);
            }

            if (!await _roles.RoleExistsAsync("Employee"))
                await _roles.CreateAsync(new IdentityRole("Employee"));

            await _users.AddToRoleAsync(user, "Employee");

            _logger.LogInformation("Empleado {Email} creado por {Admin}", user.Email, User.Identity?.Name);
            TempData["Success"] = $"✅ Empleado '{user.Email}' creado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        // POST: /Employees/ResetPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([Bind(Prefix = "ResetModel")] ResetEmployeePasswordVm model)
        {
            if (!ModelState.IsValid)
            {
                var list = await _users.GetUsersInRoleAsync("Employee");
                var vm = new EmployeesIndexVm
                {
                    Employees = list,
                    CreateModel = new CreateEmployeeVm(),
                    ResetModel = model
                };
                return View("Index", vm);
            }

            var user = await _users.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["Error"] = "❌ Empleado no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var res = await _users.ResetPasswordAsync(user, token, model.NewPassword);

            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                    ModelState.AddModelError("ResetModel.NewPassword", e.Description);

                var list = await _users.GetUsersInRoleAsync("Employee");
                var vm = new EmployeesIndexVm
                {
                    Employees = list,
                    CreateModel = new CreateEmployeeVm(),
                    ResetModel = model
                };
                return View("Index", vm);
            }

            _logger.LogInformation("Contraseña de {Email} restablecida por {Admin}", user.Email, User.Identity?.Name);
            TempData["Success"] = $"🔑 Contraseña de '{user.Email}' restablecida correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}