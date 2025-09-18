using Microsoft.AspNetCore.Identity;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Data
{
public static class IdentitySeed
{
/// <summary>
/// Crea roles base y un usuario administrador si no existen.
/// </summary>
public static async Task SeedAsync(IServiceProvider sp)
{
var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();


string[] roles = new[] { "Admin", "Employee", "Customer" };
foreach (var r in roles)
if (!await roleMgr.RoleExistsAsync(r))
await roleMgr.CreateAsync(new IdentityRole(r));


var adminEmail = "admin@tiendaplayeras.local";
var admin = await userMgr.FindByEmailAsync(adminEmail);
if (admin == null)
{
admin = new ApplicationUser
{
UserName = "admin",
Email = adminEmail,
EmailConfirmed = true,
FirstName = "Admin",
LastName = "Principal",
IsActive = true
};
await userMgr.CreateAsync(admin, "Admin#12345"); // cambia en producción
await userMgr.AddToRoleAsync(admin, "Admin");
}
}
}
}