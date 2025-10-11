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
public DbSet<Category> Categories => Set<Category>();
        public DbSet<Tag> Tags => Set<Tag>();

        public DbSet<ProductTag> ProductTags => Set<ProductTag>();



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // === PRODUCTOS ===
            builder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
            builder.Entity<Product>().HasIndex(p => p.Name).IsUnique(); // Nueva restricción
            builder.Entity<Product>().HasIndex(p => p.IsActive);

            // === VARIANTES DE PRODUCTO ===
            builder.Entity<ProductVariant>().HasIndex(v => new { v.ProductId, v.Size, v.Fit, v.Color, v.DesignCode }).IsUnique();

            // === WISHLIST ===
            builder.Entity<WishlistItem>().HasIndex(w => new { w.UserId, w.ProductVariantId }).IsUnique();

            // === CATEGORÍAS ===
            builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            builder.Entity<Category>().HasIndex(c => c.Name).IsUnique(); // Nueva restricción
            builder.Entity<Category>().HasIndex(c => c.IsActive);

            // === ETIQUETAS (TAGS) ===
            builder.Entity<Tag>().HasIndex(t => new { t.CategoryId, t.Slug }).IsUnique();
            builder.Entity<Tag>().HasIndex(t => new { t.CategoryId, t.Name }).IsUnique(); // Nueva restricción
            builder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique(); // Slug único globalmente
            builder.Entity<Tag>().HasIndex(t => t.IsActive);

            // === RELACIÓN PRODUCTO-TAG (N:N) ===
            builder.Entity<ProductTag>()
                .HasKey(pt => new { pt.ProductId, pt.TagId });

            builder.Entity<ProductTag>()
                .HasIndex(pt => new { pt.ProductId, pt.TagId })
                .IsUnique();

            builder.Entity<ProductTag>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductTags)
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.ProductTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductTag>()
                .HasIndex(pt => pt.IsActive);

            // Configuraciones adicionales para mejorar performance
            builder.Entity<Product>()
                .Property(p => p.BasePrice)
                .HasColumnType("numeric(10,2)");

            builder.Entity<ProductVariant>()
                .Property(p => p.Price)
                .HasColumnType("numeric(10,2)");

            // Configuraciones de strings para evitar truncamiento
            builder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Entity<Product>()
                .Property(p => p.Slug)
                .HasMaxLength(140)
                .IsRequired();

            builder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(120)
                .IsRequired();

            builder.Entity<Category>()
                .Property(c => c.Slug)
                .HasMaxLength(140)
                .IsRequired();

            builder.Entity<Tag>()
                .Property(t => t.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Entity<Tag>()
                .Property(t => t.Slug)
                .HasMaxLength(140)
                .IsRequired();

            // Configuración de fechas con valores por defecto para PostgreSQL
            builder.Entity<Product>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("NOW()");

            builder.Entity<Category>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("NOW()");

            builder.Entity<Tag>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("NOW()");

            builder.Entity<ProductTag>()
                .Property(pt => pt.CreatedAt)
                .HasDefaultValueSql("NOW()");

            builder.Entity<Product>().Property(p => p.AllowedFitsCsv).HasMaxLength(200);
            builder.Entity<Product>().Property(p => p.AllowedColorsCsv).HasMaxLength(400);
            builder.Entity<Product>().Property(p => p.AllowedSizesCsv).HasMaxLength(200);

builder.Entity<Product>()
    .Property(p => p.AllowedFitsCsv)
    .HasMaxLength(300);

builder.Entity<Product>()
    .Property(p => p.AllowedColorsCsv)
    .HasMaxLength(300);

builder.Entity<Product>()
    .Property(p => p.AllowedSizesCsv)
    .HasMaxLength(300);

        }
}
}