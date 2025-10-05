using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TiendaPlayeras.Web.Models;


namespace TiendaPlayeras.Web.Data
{
/// <summary>
/// DbContext principal. Integra Identity + entidades de tienda.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
: base(options) { }


public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
public DbSet<Product> Products => Set<Product>();
public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
public DbSet<DesignUpload> DesignUploads => Set<DesignUpload>();
public DbSet<Cart> Carts => Set<Cart>();
public DbSet<CartItem> CartItems => Set<CartItem>();
public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderItem> OrderItems => Set<OrderItem>();


protected override void OnModelCreating(ModelBuilder builder)
{
base.OnModelCreating(builder);
// Índices y restricciones básicas
builder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
builder.Entity<ProductVariant>().HasIndex(v => new { v.ProductId, v.Size, v.Fit, v.Color, v.DesignCode }).IsUnique();
builder.Entity<WishlistItem>().HasIndex(w => new { w.UserId, w.ProductVariantId }).IsUnique();
}
}
}