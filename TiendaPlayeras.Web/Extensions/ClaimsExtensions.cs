using System.Security.Claims;


namespace TiendaPlayeras.Web
{
/// <summary>Extensiones para ClaimsPrincipal (obtener UserId).</summary>
public static class ClaimsExtensions
{
public static string? GetUserId(this ClaimsPrincipal user) =>
user?.FindFirstValue(ClaimTypes.NameIdentifier);
}
}