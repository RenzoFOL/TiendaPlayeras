using System.ComponentModel.DataAnnotations;

namespace TiendaPlayeras.Web.Models.Account
{
    public class ProfileVm
    {
        [Display(Name = "Nombre")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Apellido paterno")]
        [StringLength(100)]
        public string? LastNamePaterno { get; set; }

        [Display(Name = "Apellido materno")]
        [StringLength(100)]
        public string? LastNameMaterno { get; set; }


        [Display(Name = "Apellidos")]
        [StringLength(200)]
        public string? LastNames { get; set; }


        [Display(Name = "Teléfono")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Correo")]
        [EmailAddress]
        public string Email { get; set; } = "";

        // Opcional: para UI de cambio de contraseña (no funcional aún)
        [Display(Name = "Nueva contraseña")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirmar nueva contraseña")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmPassword { get; set; }
    }
}
