using Microsoft.AspNetCore.Identity;

namespace TiendaPlayeras.Web.Services
{
    public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new() { Code = nameof(DefaultError), Description = "Se produjo un error desconocido." };

        public override IdentityError ConcurrencyFailure() =>
            new() { Code = nameof(ConcurrencyFailure), Description = "Error de concurrencia: los datos fueron modificados por otro proceso." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "La contraseña actual no es correcta." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "El token es inválido o ha expirado." };

        public override IdentityError LoginAlreadyAssociated() =>
            new() { Code = nameof(LoginAlreadyAssociated), Description = "Este inicio de sesión externo ya está asociado a una cuenta." };

        public override IdentityError InvalidUserName(string userName) =>
            new() { Code = nameof(InvalidUserName), Description = $"El nombre de usuario '{userName}' no es válido." };

        public override IdentityError InvalidEmail(string email) =>
            new() { Code = nameof(InvalidEmail), Description = $"El correo '{email}' no es válido." };

        public override IdentityError DuplicateUserName(string userName) =>
            new() { Code = nameof(DuplicateUserName), Description = $"El usuario '{userName}' ya existe." };

        public override IdentityError DuplicateEmail(string email) =>
            new() { Code = nameof(DuplicateEmail), Description = $"El correo '{email}' ya está en uso." };

        public override IdentityError InvalidRoleName(string role) =>
            new() { Code = nameof(InvalidRoleName), Description = $"El nombre de rol '{role}' no es válido." };

        public override IdentityError DuplicateRoleName(string role) =>
            new() { Code = nameof(DuplicateRoleName), Description = $"El rol '{role}' ya existe." };

        public override IdentityError UserAlreadyHasPassword() =>
            new() { Code = nameof(UserAlreadyHasPassword), Description = "El usuario ya tiene una contraseña establecida." };

        public override IdentityError UserLockoutNotEnabled() =>
            new() { Code = nameof(UserLockoutNotEnabled), Description = "El bloqueo de cuenta no está habilitado para este usuario." };

        public override IdentityError UserAlreadyInRole(string role) =>
            new() { Code = nameof(UserAlreadyInRole), Description = $"El usuario ya pertenece al rol '{role}'." };

        public override IdentityError UserNotInRole(string role) =>
            new() { Code = nameof(UserNotInRole), Description = $"El usuario no pertenece al rol '{role}'." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"La contraseña es demasiado corta. Debe tener al menos {length} caracteres." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe incluir al menos un carácter no alfanumérico." };

        public override IdentityError PasswordRequiresDigit() =>
            new() { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe incluir al menos un número." };

        public override IdentityError PasswordRequiresLower() =>
            new() { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe incluir al menos una letra minúscula." };

        public override IdentityError PasswordRequiresUpper() =>
            new() { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe incluir al menos una letra mayúscula." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"La contraseña debe contener al menos {uniqueChars} caracteres únicos." };

        public override IdentityError RecoveryCodeRedemptionFailed() =>
            new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "El código de recuperación es inválido." };
    }
}
