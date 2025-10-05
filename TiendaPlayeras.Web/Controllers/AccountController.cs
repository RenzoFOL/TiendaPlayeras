using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web;
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Services;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>
    /// Maneja login/registro/olvido/verificación y redirección por rol.
    /// Todas las acciones POST devuelven JSON para integrarse con una vista dinámica (sin recarga).
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailSender _email;
        private readonly ICartService _cart; // para merge de carrito invitado → usuario

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailSender email,
            ICartService cart)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _email = email;
            _cart = cart;
        }

        /// <summary>
        /// Muestra la página dinámica de cuenta (login/registro/olvido).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            // Si ya está autenticado, NO mostramos /Account; redirigimos por rol
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return LocalRedirect("/Admin");
                if (User.IsInRole("Employee")) return LocalRedirect("/Employee");
                return LocalRedirect("/"); // Customer
            }
            ViewBag.ReturnUrl = returnUrl;
            return View("Account"); // Views/Account/Account.cshtml
        }


        /// <summary>
        /// Inicia sesión y devuelve JSON con redirectUrl según rol.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Json(new { ok = false, error = "Email y contraseña son obligatorios." });

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Json(new { ok = false, error = "Credenciales inválidas." });

            if (!user.IsActive) return Json(new { ok = false, error = "Tu cuenta está inhabilitada. Contacta al administrador." });

            // Si exiges confirmación de correo:
            if (!_userManager.Options.SignIn.RequireConfirmedEmail || user.EmailConfirmed)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    if (result.IsLockedOut) return Json(new { ok = false, error = "Cuenta bloqueada temporalmente." });
                    return Json(new { ok = false, error = "Credenciales inválidas." });
                }

                // Merge carrito invitado → usuario
                try { await _cart.MergeGuestCartToUserAsync(HttpContext.Session.Id, user.Id); } catch { /* opcional: log */ }

                // Redirección por rol
                var roles = await _userManager.GetRolesAsync(user);
                var redirect = RoleRedirect(roles);
                return Json(new { ok = true, redirectUrl = redirect });
            }
            else
            {
                // Enviar ícono para reenvío si quieres; de momento solo mensaje
                return Json(new { ok = false, error = "Debes confirmar tu correo para iniciar sesión." });
            }
        }
[Authorize]
[HttpGet]
public IActionResult Profile()
{
    return View(); // crea Views/Account/Profile.cshtml con tu UI de edición
}

        /// <summary>
        /// Registro de cliente (auto-registro siempre rol "Customer").
        /// Envía email de verificación con token.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromForm] RegisterVm vm)
        {
            if (!ModelState.IsValid)
                return Json(new { ok = false, error = "Datos de registro inválidos." });

            var exists = await _userManager.FindByEmailAsync(vm.Email);
            if (exists != null) return Json(new { ok = false, error = "El correo ya está registrado." });

            var user = new ApplicationUser
            {
                UserName = vm.Email, // puedes cambiar a username propio si lo manejas
                Email = vm.Email,
                FirstName = vm.FirstName?.Trim() ?? "",
                LastName = vm.LastName?.Trim() ?? "",
                PhoneNumber = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim(),
                IsActive = true
            };

            var create = await _userManager.CreateAsync(user, vm.Password);
            if (!create.Succeeded)
                return Json(new { ok = false, error = string.Join("; ", create.Errors.Select(e => e.Description)) });

            // Rol por defecto: Customer
            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            await _userManager.AddToRoleAsync(user, "Customer");

            // Si no requiere confirmación de email, inicia sesión automáticamente
            if (!_userManager.Options.SignIn.RequireConfirmedEmail)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Json(new { ok = true, redirectUrl = "/" });
            }

            // Si requiere confirmación de email, envía el correo de confirmación
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var url = Url.Action(nameof(ConfirmEmail), "Account",
                    new { userId = user.Id, token = HttpUtility.UrlEncode(token) },
                    protocol: Request.Scheme);

                await _email.SendAsync(user.Email!, "Confirma tu correo",
                    $@"<p>Hola {user.FirstName},</p>
                    <p>Confirma tu correo haciendo clic:</p>
                    <p><a href=""{url}"">Confirmar correo</a></p>");

                return Json(new { ok = true, message = "Registro exitoso. Revisa tu correo para confirmar tu cuenta." });
            }
            catch (Exception ex)
            {
                // Log del error (opcional)
                // _logger.LogError(ex, "Error enviando email de confirmación para {Email}", user.Email);
                
                return Json(new { ok = true, message = "Registro exitoso, pero hubo un error al enviar el correo de confirmación. Contacta al administrador." });
            }
        }

        /// <summary>
        /// Confirmación de email desde enlace del correo.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Redirect("/Account?confirmed=0");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded) return Redirect("/Account?confirmed=1");

            return Redirect("/Account?confirmed=0");
        }

        /// <summary>
        /// Envío de enlace para restablecer contraseña.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromForm] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.EmailConfirmed)
                return Json(new { ok = true, message = "Si el correo existe y está confirmado, enviaremos un enlace." }); // no revelar

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, token = HttpUtility.UrlEncode(token) },
                protocol: Request.Scheme);

            await _email.SendAsync(user.Email!, "Restablecer contraseña",
                $@"<p>Hola,</p><p>Para restablecer tu contraseña entra aquí:</p><p><a href=""{url}"">Restablecer contraseña</a></p>");

            return Json(new { ok = true, message = "Hemos enviado un enlace para restablecer tu contraseña (si el correo existe y está confirmado)." });
        }

        /// <summary>
        /// Vista de formulario para restablecer contraseña (si prefieres, puedes integrarlo en Account.cshtml detectando querystring).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            return View("ResetPassword", new ResetPasswordVm { Email = email, Token = token });
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Status()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c=>c.Value).ToList();
                var redirect = roles.Contains("Admin") ? "/Admin" : roles.Contains("Employee") ? "/Employee" : "/";
                return Json(new { authenticated = true, roles, redirect });
            }
            return Json(new { authenticated = false });
        }

        /// <summary>
        /// Procesa restablecimiento de contraseña.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordVm vm)
        {
            if (!ModelState.IsValid)
                return Json(new { ok = false, error = "Datos inválidos." });

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null) return Json(new { ok = true, message = "Contraseña actualizada." }); // no revelar

            var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.Password);
            if (result.Succeeded) return Json(new { ok = true, redirectUrl = "/Account?reset=ok" });

            return Json(new { ok = false, error = string.Join("; ", result.Errors.Select(e => e.Description)) });
        }

        /// <summary>
        /// Cerrar sesión.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();      // limpia cookie de Identity
            return Redirect("/");                     // redirige a Home
        }


        /// <summary>Devuelve ruta por rol.</summary>
        private static string RoleRedirect(IList<string> roles)
        {
            if (roles.Contains("Admin")) return "/Admin";
            if (roles.Contains("Employee")) return "/Employee";
            return "/"; // cliente/tienda
        }
    }

    // ---------- ViewModels ----------
    public class RegisterVm
    {
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
        public string Email      { get; set; } = string.Empty;
        public string Password   { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class ResetPasswordVm
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
