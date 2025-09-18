using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);


// 1) DbContext PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// 2) Identity
builder.Services
.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
opt.SignIn.RequireConfirmedEmail = true; // verificación SMTP
opt.User.RequireUniqueEmail = true;
opt.Password.RequiredLength = 6; // ajusta según política
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();


// 3) MVC + Razor
builder.Services.AddControllersWithViews();


// 4) Límite de carga (10 MB)
builder.Services.Configure<FormOptions>(o =>
{
o.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});


var app = builder.Build();


// Migraciones automáticas (opcional en desarrollo)
using (var scope = app.Services.CreateScope())
{
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
db.Database.Migrate();
await IdentitySeed.SeedAsync(scope.ServiceProvider); // <-- agrega await y haz Program.cs async
}


if (!app.Environment.IsDevelopment())
{
app.UseExceptionHandler("/Home/Error");
app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages(); // Identity UI


app.Run();