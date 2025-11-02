using System.ComponentModel.DataAnnotations;

namespace TiendaPlayeras.Web.Models
{
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
