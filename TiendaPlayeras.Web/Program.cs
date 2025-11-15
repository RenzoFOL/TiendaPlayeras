using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using TiendaPlayeras.Web.Data;
using TiendaPlayeras.Web.Models;
using TiendaPlayeras.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Identity (con confirmación de correo)
var requireConfirmed = builder.Configuration.GetValue<bool>("Auth:RequireConfirmedEmail", false);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opt =>
    {
        opt.SignIn.RequireConfirmedEmail = requireConfirmed;
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account";
    opt.LogoutPath = "/Account/Logout";
    opt.AccessDeniedPath = "/Account";
});

// ⛑️ Anti-Forgery: usa la cabecera que envía tu JS
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// 3) MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4) Servicios personalizados
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<EmailSender>(); // MailKit sender

// 5) Carrito (DI)
builder.Services.AddScoped<ICartService, CartService>();

// 6) Límite de carga (10 MB para diseños)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddSession();
var app = builder.Build();

// 7) Migraciones + seeding de roles/usuario admin (dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await IdentitySeed.SeedAsync(scope.ServiceProvider); // seeding (roles + admin)
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 8) Rutas MVC por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 9) Identity UI (Razor Pages)
app.MapRazorPages();

app.Run();
