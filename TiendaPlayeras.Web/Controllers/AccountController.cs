using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using System.Web;
using System.Security.Cryptography;
using System.Text;
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
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        private const string SecQSessionPrefix = "SECQ_OK_";
        
        private static readonly Dictionary<string, string> _secQuestions = new()
        {
            { "pet",     "¿Cuál fue el nombre de tu primera mascota?" },
            { "school",  "¿En qué primaria estudiaste?" },
            { "city",    "¿En qué ciudad nació tu madre?" },
            { "color",   "¿Cuál es tu color favorito?" },
            { "nick",    "¿Cuál era tu apodo en la infancia?" }
        };

        private static string HashAnswer(string? answer)
        {
            var clean = (answer ?? "").Trim().ToLowerInvariant();
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(clean));
            return Convert.ToHexString(bytes);
        }

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailSender email,
            IWebHostEnvironment env,
            ApplicationDbContext context)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _roleManager   = roleManager;
            _email         = email;
            _env           = env;
        }

        // ======== Auth ========

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null, string? tab = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return LocalRedirect("/Admin");
                if (User.IsInRole("Employee")) return LocalRedirect("/Employee");
                return LocalRedirect("/");
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.ActiveTab = tab; // para activar pestañas (login/forgot)
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

            // Validación mínima de la pregunta/respuesta (solo para nuevos)
            if (string.IsNullOrWhiteSpace(vm.SecurityQuestionKey))
                return Json(new { ok = false, error = "Selecciona una pregunta de seguridad." });

            if (string.IsNullOrWhiteSpace(vm.SecurityAnswer))
                return Json(new { ok = false, error = "Escribe la respuesta a tu pregunta de seguridad." });

            // Crea el usuario
            var user = new ApplicationUser
            {
                UserName    = vm.Email,
                Email       = vm.Email,
                FirstName   = vm.FirstName?.Trim() ?? "",
                LastName    = vm.LastName?.Trim() ?? "",
                PhoneNumber = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim(),
                IsActive    = true,

                // Guardamos la pregunta + hash de la respuesta (normalizada a minúsculas)
                SecurityQuestionKey = vm.SecurityQuestionKey.Trim(),
                SecurityAnswerHash  = HashAnswer(vm.SecurityAnswer) // usa tu helper SHA-256
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

        [AllowAnonymous]
        [HttpGet("account/forgot-check-email")]
        public async Task<IActionResult> ForgotCheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { ok = false, error = "Ingresa tu correo." });

            var norm = email.Trim().ToUpperInvariant();
            var user = await _userManager.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == norm || u.NormalizedUserName == norm);

            if (user == null)
                return Json(new { ok = false, error = "No encontramos ese correo." });

            if (string.IsNullOrWhiteSpace(user.SecurityQuestionKey) ||
                string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
            {
                return Json(new
                {
                    ok = false,
                    needsSetup = true,
                    error = "Esta cuenta aún no tiene pregunta de seguridad configurada. Inicia sesión y configúrala en tu perfil."
                });
            }

            var questionText = _secQuestions.TryGetValue(user.SecurityQuestionKey, out var txt) ? txt : "Pregunta de seguridad";

            // limpiar verificación previa
            HttpContext.Session.Remove($"{SecQSessionPrefix}{email.Trim()}");

            return Json(new { ok = true, questionText });
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

        /// <summary>
        /// 🔐 Restablece la contraseña en la misma vista (email + nueva contraseña).
        /// Valida que el email exista y usa el flujo oficial de Identity (token interno).
        /// </summary>

        [AllowAnonymous]
        [HttpPost("account/reset-direct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordDirect([FromForm] ResetPasswordDirectVM vm)
        {
            vm.Email = (vm.Email ?? "").Trim();
            vm.Password = vm.Password?.Trim() ?? "";
            vm.ConfirmPassword = vm.ConfirmPassword?.Trim() ?? "";

            if (!ModelState.IsValid)
                return View("Account");

            var user = await _userManager.FindByEmailAsync(vm.Email) ?? await _userManager.FindByNameAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError(nameof(vm.Email), "No existe una cuenta con ese correo.");
                return View("Account");
            }

            var verified = HttpContext.Session.GetString($"{SecQSessionPrefix}{vm.Email}") == "1";
            if (!verified)
            {
                ModelState.AddModelError(string.Empty, "Primero debes responder correctamente tu pregunta de seguridad.");
                return View("Account");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View("Account");
            }

            await _userManager.UpdateSecurityStampAsync(user);
            TempData["ResetOk"] = "Tu contraseña se actualizó correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Index), new { tab = "login" });
        }

        public class ResetPasswordDirectVM
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
        }

        [AllowAnonymous]
        [HttpGet("account/email-exists")]
        public async Task<IActionResult> EmailExists([FromQuery] string email)
        {
            email = (email ?? "").Trim();
            if (string.IsNullOrEmpty(email))
                return Json(new { exists = false });

            var user = await _userManager.FindByEmailAsync(email)
                    ?? await _userManager.FindByNameAsync(email);

            return Json(new { exists = user != null });
        }

        [AllowAnonymous]
        [HttpGet("account/security-question")]
        public async Task<IActionResult> GetSecurityQuestion([FromQuery] string email)
        {
            var mail = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(mail))
                return Json(new { ok = false, error = "Ingresa tu correo." });

            var user = await _userManager.FindByEmailAsync(mail)
                    ?? await _userManager.FindByNameAsync(mail);

            if (user == null)
                return Json(new { ok = false, error = "No existe una cuenta con ese correo." });

            if (string.IsNullOrWhiteSpace(user.SecurityQuestionKey))
                return Json(new { ok = false, error = "Esta cuenta no tiene pregunta de seguridad registrada." });

            var question = _secQuestions.TryGetValue(user.SecurityQuestionKey, out var txt)
                ? txt : "Pregunta no definida";

            return Json(new { ok = true, question });
        }

        // GET: /Account/OrdersPartial
[Authorize]
[HttpGet("account/orders-partial")]
public async Task<IActionResult> OrdersPartial()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Unauthorized();

    // Usar el mismo contexto que tienes en MyOrdersController
    // Necesitarás inyectar ApplicationDbContext en AccountController
    var orders = await _context.Orders
        .Where(o => o.UserId == user.Id)
        .Include(o => o.Items)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    return PartialView("_OrdersPartial", orders);
}

        [AllowAnonymous]
        [HttpPost("account/verify-security-answer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifySecurityAnswer([FromBody] SecQAnswerReq dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Answer))
                return Json(new { ok = false, error = "Datos incompletos." });

            var norm = dto.Email.Trim().ToUpperInvariant();
            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.NormalizedEmail == norm || u.NormalizedUserName == norm);

            if (user == null)
                return Json(new { ok = false, error = "Correo no encontrado." });

            if (string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
                return Json(new { ok = false, error = "No hay pregunta de seguridad configurada en esta cuenta." });

            var ok = string.Equals(user.SecurityAnswerHash, HashAnswer(dto.Answer), StringComparison.OrdinalIgnoreCase);
            if (!ok)
                return Json(new { ok = false, error = "Respuesta incorrecta." });

            HttpContext.Session.SetString($"{SecQSessionPrefix}{dto.Email.Trim()}", "1");
            return Json(new { ok = true });
        }

        public class SecQAnswerReq
        {
            public string Email { get; set; } = "";
            public string Answer { get; set; } = "";
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


        private readonly PasswordHasher<ApplicationUser> _answerHasher = new();

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

        [Required, EmailAddress]
        public string  Email     { get; set; } = string.Empty;

        [Required]
        public string  Password  { get; set; } = string.Empty;

        [Required]
        public string  ConfirmPassword { get; set; } = string.Empty;

        public string? Phone     { get; set; }

        // === nuevos campos para pregunta de seguridad ===
        [Required]
        public string SecurityQuestionKey { get; set; } = string.Empty; // ej. "pet", "school"...

        [Required, StringLength(200)]
        public string SecurityAnswer { get; set; } = string.Empty;      // respuesta libre del usuario
    }

    public class ResetPasswordVm
    {
        public string Email           { get; set; } = string.Empty;
        public string Token           { get; set; } = string.Empty;
        public string Password        { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// VM para el restablecimiento directo (email + nueva contraseña en la misma vista).
    /// </summary>
    public class ResetPasswordDirectVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password),
            ErrorMessage = "La confirmación no coincide.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}