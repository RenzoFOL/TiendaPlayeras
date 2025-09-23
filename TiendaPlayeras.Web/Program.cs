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
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opt =>
    {
        opt.SignIn.RequireConfirmedEmail = true; // forzar verificación
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 2.1) Cookies de autenticación: rutas del flujo de acceso
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account";          // login UI centralizada
    opt.LogoutPath = "/Account/Logout";  // logout
    opt.AccessDeniedPath = "/Account";   // acceso denegado → página de cuenta
});

// 3) MVC + Razor Pages (para Identity UI)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4) Servicios personalizados
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddSingleton<EmailSender>(); // MailKit sender

// 5) Límite de carga (10 MB para diseños)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

// 6) Session (carrito invitado)
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".TiendaPlayeras.Session"; // nombre de cookie de sesión
    options.IdleTimeout = TimeSpan.FromHours(12);    // expira tras 12 h de inactividad
    options.Cookie.HttpOnly = true;                  // mitiga XSS
    options.Cookie.IsEssential = true;               // requerida para funcionalidad básica
});

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
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session antes de Auth si la usas en eventos de login (merge de carrito)
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
