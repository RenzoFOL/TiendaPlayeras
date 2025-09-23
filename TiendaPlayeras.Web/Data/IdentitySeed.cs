using Microsoft.AspNetCore.Identity;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Data
{
public static class IdentitySeed
{
/// <summary>
/// Crea roles base y un usuario administrador si no existen.
/// </summary>
public static async Task SeedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Roles
    foreach (var role in new[] { "Admin", "Employee", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Admin
    if (await userManager.FindByEmailAsync("admin@test.com") == null)
    {
        var u = new ApplicationUser
        {
            UserName = "admin@test.com",
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "Prueba",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(u, "Admin#123");
        await userManager.AddToRoleAsync(u, "Admin");
    }

    // Employee
    if (await userManager.FindByEmailAsync("empleado@test.com") == null)
    {
        var u = new ApplicationUser
        {
            UserName = "empleado@test.com",
            Email = "empleado@test.com",
            FirstName = "Empleado",
            LastName = "Prueba",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(u, "Empleado#123");
        await userManager.AddToRoleAsync(u, "Employee");
    }

    // Customer
    if (await userManager.FindByEmailAsync("cliente@test.com") == null)
    {
        var u = new ApplicationUser
        {
            UserName = "cliente@test.com",
            Email = "cliente@test.com",
            FirstName = "Cliente",
            LastName = "Prueba",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(u, "Cliente#123");
        await userManager.AddToRoleAsync(u, "Customer");
    }
}

}
}