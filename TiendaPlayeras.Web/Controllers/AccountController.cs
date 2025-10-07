using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Web; // si prefieres WebUtility, cambia a System.Net y usa WebUtility.UrlEncode
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Services;

namespace TiendaPlayeras.Web.Controllers
{
    /// <summary>
    /// Maneja login/registro/olvido/verificación y perfil.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailSender _email;
        private readonly ICartService _cart;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailSender email,
            ICartService cart,
            IWebHostEnvironment env)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _roleManager   = roleManager;
            _email         = email;
            _cart          = cart;
            _env           = env;
        }

        // ======== Auth ========

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return LocalRedirect("/Admin");
                if (User.IsInRole("Employee")) return LocalRedirect("/Employee");
                return LocalRedirect("/");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View("Account");
        }

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

            if (!_userManager.Options.SignIn.RequireConfirmedEmail || user.EmailConfirmed)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
                if (!result.Succeeded)
                    return Json(new { ok = false, error = result.IsLockedOut ? "Cuenta bloqueada temporalmente." : "Credenciales inválidas." });

                try { await _cart.MergeGuestCartToUserAsync(HttpContext.Session.Id, user.Id); } catch { }

                var roles = await _userManager.GetRolesAsync(user);
                var redirect = RoleRedirect(roles);
                return Json(new { ok = true, redirectUrl = redirect });
            }

            return Json(new { ok = false, error = "Debes confirmar tu correo para iniciar sesión." });
        }

        // ======== Perfil (vista) ========

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            var vm = new TiendaPlayeras.Web.Models.Account.ProfileVm
            {
                FirstName   = user.FirstName,
                LastNames   = user.LastName,      // ambos apellidos en un campo
                PhoneNumber = user.PhoneNumber ?? "",
                Email       = user.Email ?? ""
            };
            return View(vm);
        }

        [AllowAnonymous]
        [HttpGet("account/profile-preview")]
        public IActionResult ProfilePreview()
        {
            if (!_env.IsDevelopment()) return NotFound();

            var vm = new TiendaPlayeras.Web.Models.Account.ProfileVm
            {
                FirstName   = "Nombre",
                LastNames   = "Paterno Materno",
                PhoneNumber = "5551234567",
                Email       = "cliente@demo.com"
            };
            return View("Profile", vm);
        }

        // ======== Registro / Confirmación / Password reset ========

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
                UserName    = vm.Email,
                Email       = vm.Email,
                FirstName   = vm.FirstName?.Trim() ?? "",
                LastName    = vm.LastName?.Trim() ?? "",
                PhoneNumber = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim(),
                IsActive    = true
            };

            var create = await _userManager.CreateAsync(user, vm.Password);
            if (!create.Succeeded)
                return Json(new { ok = false, error = string.Join("; ", create.Errors.Select(e => e.Description)) });

            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
            await _userManager.AddToRoleAsync(user, "Customer");

            if (!_userManager.Options.SignIn.RequireConfirmedEmail)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Json(new { ok = true, redirectUrl = "/" });
            }

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
            catch
            {
                return Json(new { ok = true, message = "Registro exitoso, pero hubo un error al enviar el correo de confirmación." });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Redirect("/Account?confirmed=0");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return Redirect(result.Succeeded ? "/Account?confirmed=1" : "/Account?confirmed=0");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromForm] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.EmailConfirmed)
                return Json(new { ok = true, message = "Si el correo existe y está confirmado, enviaremos un enlace." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, token = HttpUtility.UrlEncode(token) },
                protocol: Request.Scheme);

            await _email.SendAsync(user.Email!, "Restablecer contraseña",
                $@"<p>Hola,</p><p>Para restablecer tu contraseña entra aquí:</p><p><a href=""{url}"">Restablecer contraseña</a></p>");

            return Json(new { ok = true, message = "Hemos enviado un enlace para restablecer tu contraseña (si el correo existe y está confirmado)." });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            return View("ResetPassword", new ResetPasswordVm { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordVm vm)
        {
            if (!ModelState.IsValid)
                return Json(new { ok = false, error = "Datos inválidos." });

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null) return Json(new { ok = true, message = "Contraseña actualizada." });

            var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.Password);
            return result.Succeeded
                ? Json(new { ok = true, redirectUrl = "/Account?reset=ok" })
                : Json(new { ok = false, error = string.Join("; ", result.Errors.Select(e => e.Description)) });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }

        private static string RoleRedirect(IList<string> roles)
        {
            if (roles.Contains("Admin")) return "/Admin";
            if (roles.Contains("Employee")) return "/Employee";
            return "/";
        }

        // ======== Perfil: actualización inline por campo (anidados en el controlador) ========

        public class UpdateFieldRequest
        {
            public string Field { get; set; } = "";
            public string Value { get; set; } = "";
        }

        [Authorize]
        [HttpPost("account/profile/update-field")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileField([FromBody] UpdateFieldRequest req)
        {
            if (req == null) return Json(new { ok = false, error = "Solicitud vacía." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { ok = false, error = "Usuario no encontrado." });

            if (string.IsNullOrWhiteSpace(req.Field))
                return Json(new { ok = false, error = "Campo inválido." });

            IdentityResult result;

            switch (req.Field.Trim().ToLowerInvariant())
            {
                case "firstname":
                    user.FirstName = (req.Value ?? "").Trim();
                    result = await _userManager.UpdateAsync(user);
                    break;

                case "lastnames":
                case "lastname":
                case "apellidos":
                    user.LastName = (req.Value ?? "").Trim();
                    result = await _userManager.UpdateAsync(user);
                    break;

                case "email":
                {
                    var email = (req.Value ?? "").Trim();
                    if (string.IsNullOrEmpty(email))
                        return Json(new { ok = false, error = "El correo no puede estar vacío." });

                    result = await _userManager.SetEmailAsync(user, email);
                    if (!result.Succeeded)
                        return Json(new { ok = false, error = string.Join("; ", result.Errors.Select(e => e.Description)) });

                    result = await _userManager.SetUserNameAsync(user, email);
                    break;
                }

                case "phonenumber":
                case "telefono":
                    result = await _userManager.SetPhoneNumberAsync(user, (req.Value ?? "").Trim());
                    break;

                default:
                    return Json(new { ok = false, error = "Campo no soportado." });
            }

            if (!result.Succeeded)
                return Json(new { ok = false, error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            return Json(new { ok = true, message = "Actualizado." });
        }

        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string ConfirmNewPassword { get; set; } = "";
        }

        [Authorize]
        [HttpPost("account/profile/change-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            if (req == null) return Json(new { ok = false, error = "Solicitud vacía." });

            if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword != req.ConfirmNewPassword)
                return Json(new { ok = false, error = "Las contraseñas no coinciden." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { ok = false, error = "Usuario no encontrado." });

            var hasPwd = await _userManager.HasPasswordAsync(user);
            IdentityResult result;

            if (!hasPwd)
            {
                result = await _userManager.AddPasswordAsync(user, req.NewPassword);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(req.CurrentPassword))
                    return Json(new { ok = false, error = "Debes ingresar tu contraseña actual." });

                result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            }

            if (!result.Succeeded)
                return Json(new { ok = false, error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            return Json(new { ok = true, message = "Contraseña actualizada." });
        }
    [Authorize]
    [HttpPost("account/profile/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Json(new { ok = false, error = "Usuario no encontrado." });

    var result = await _userManager.DeleteAsync(user);
    if (!result.Succeeded)
        return Json(new { ok = false, error = string.Join("; ", result.Errors.Select(e => e.Description)) });

    await _signInManager.SignOutAsync();
    return Json(new { ok = true, redirectUrl = Url.Content("~/") });
}

    }

    // ---------- ViewModels (pueden estar aquí fuera del controlador) ----------
    public class RegisterVm
    {
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
        public string  Email     { get; set; } = string.Empty;
        public string  Password  { get; set; } = string.Empty;
        public string  ConfirmPassword { get; set; } = string.Empty;
        public string? Phone     { get; set; }
    }

    public class ResetPasswordVm
    {
        public string Email           { get; set; } = string.Empty;
        public string Token           { get; set; } = string.Empty;
        public string Password        { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
