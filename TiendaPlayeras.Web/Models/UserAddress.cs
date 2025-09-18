namespace TiendaPlayeras.Web.Models
{
/// <summary>Direcciones de envío/facturación del usuario.</summary>
public class UserAddress
{
public int Id { get; set; }
public string UserId { get; set; } = string.Empty;
public ApplicationUser? User { get; set; }
public string Line1 { get; set; } = string.Empty; // Calle y número
public string? Line2 { get; set; } // Interior, referencias
public string City { get; set; } = string.Empty;
public string State { get; set; } = string.Empty;
public string PostalCode { get; set; } = string.Empty;
public string Country { get; set; } = "MX";
public bool IsDefault { get; set; } = false;
public bool IsActive { get; set; } = true; // inhabilitación lógica
}
}