using System.ComponentModel.DataAnnotations;
using TiendaPlayeras.Web.Models;

namespace TiendaPlayeras.Web.Models.Employees
{
    public class EmployeesIndexVm
    {
        public IEnumerable<ApplicationUser> Employees { get; set; } = new List<ApplicationUser>();
        public CreateEmployeeVm CreateModel { get; set; } = new();
        public ResetEmployeePasswordVm ResetModel { get; set; } = new();
    }

    public class CreateEmployeeVm
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string? ConfirmPassword { get; set; }
    }

    public class ResetEmployeePasswordVm
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes confirmar la contraseña")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
