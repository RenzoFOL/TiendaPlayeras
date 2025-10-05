using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;


namespace TiendaPlayeras.Web.Models
{
/// <summary>
/// Usuario de la aplicación. Hereda de IdentityUser y agrega nombres y estado.
/// </summary>
public class ApplicationUser : IdentityUser
{
/// <summary>Nombre(s) del usuario.</summary>
public string FirstName { get; set; } = string.Empty;
/// <summary>Apellidos del usuario.</summary>
public string LastName { get; set; } = string.Empty;
/// <summary>Inhabilitación lógica (true = activo, false = inhabilitado).</summary>
public bool IsActive { get; set; } = true;
/// <summary>Direcciones del usuario.</summary>
public List<UserAddress> Addresses { get; set; } = new();
}
}