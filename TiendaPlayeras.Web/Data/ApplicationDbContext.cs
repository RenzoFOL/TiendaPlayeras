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

        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
        public DbSet<Product> Products => Set<Product>();

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
            builder.Entity<Product>().HasIndex(p => p.Name).IsUnique();
            builder.Entity<Product>().HasIndex(p => p.IsActive);

            // Configurar AvailableSizes con valor por defecto
            builder.Entity<Product>()
                .Property(p => p.AvailableSizes)
                .HasDefaultValue("S,M,L,XL")
                .HasMaxLength(50);

            // === IMÁGENES DE PRODUCTO ===
            builder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(pi => pi.Product)
                    .WithMany(p => p.ProductImages)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(pi => pi.ProductId);
                entity.HasIndex(pi => pi.DisplayOrder);
                entity.HasIndex(pi => pi.IsMain);

                // Configurar valor por defecto para CreatedAt
                entity.Property(pi => pi.CreatedAt)
                    .HasDefaultValueSql("NOW()");
            });

            // === ÓRDENES ===
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.HasOne(o => o.User)
                    .WithMany()
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.ShippingAddress)
                    .WithMany()
                    .HasForeignKey(o => o.ShippingAddressId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(o => o.UserId);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.CreatedAt);
                entity.HasIndex(o => o.IsActive);

                // Configurar valores por defecto
                entity.Property(o => o.CreatedAt)
                    .HasDefaultValueSql("NOW()");

                entity.Property(o => o.Status)
                    .HasDefaultValue("Pending");

                entity.Property(o => o.IsActive)
                    .HasDefaultValue(true);
            });

            // === ITEMS DE ORDEN ===
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ ACTUALIZADO: Relación con Product
                entity.HasOne(oi => oi.Product)
                    .WithMany()
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(oi => oi.OrderId);
                entity.HasIndex(oi => oi.ProductId);
                entity.HasIndex(oi => oi.IsActive);

                // Configurar tamaño para Size
                entity.Property(oi => oi.Size)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(oi => oi.IsActive)
                    .HasDefaultValue(true);
            });

            // === CARRITO ===
            builder.Entity<Cart>(entity =>
            {
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.SessionId);
                entity.HasIndex(c => c.IsActive);

                // Un usuario puede tener solo un carrito activo
                entity.HasIndex(c => new { c.UserId, c.IsActive })
                    .IsUnique()
                    .HasFilter("\"UserId\" IS NOT NULL");
            });

            builder.Entity<CartItem>(entity =>
            {
                entity.HasKey(ci => ci.Id);

                // Relación con Cart
                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.Items)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ CAMBIO: Relación con Product (no con ProductVariant)
                entity.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict); // No eliminar si el producto se elimina

                // Índice único: no puede haber el mismo producto+talla en el mismo carrito
                entity.HasIndex(ci => new { ci.CartId, ci.ProductId, ci.Size })
                    .IsUnique();

                entity.HasIndex(ci => ci.IsActive);

                // Configurar tamaño máximo para Size
                entity.Property(ci => ci.Size)
                    .HasMaxLength(10)
                    .IsRequired();
            });

            // === WISHLIST (productos simples) ===
            builder.Entity<WishlistItem>(entity =>
            {
                entity.HasKey(w => w.Id);

                // Relación con Product
                entity.HasOne(w => w.Product)
                    .WithMany()
                    .HasForeignKey(w => w.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Un producto solo puede estar una vez en la wishlist de un usuario
                entity.HasIndex(w => new { w.UserId, w.ProductId })
                    .IsUnique();

                entity.HasIndex(w => w.IsActive);
            });

            // === CATEGORÍAS ===
            builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            builder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            builder.Entity<Category>().HasIndex(c => c.IsActive);

            // === ETIQUETAS (TAGS) ===
            builder.Entity<Tag>().HasIndex(t => new { t.CategoryId, t.Slug }).IsUnique();
            builder.Entity<Tag>().HasIndex(t => new { t.CategoryId, t.Name }).IsUnique();
            builder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
            builder.Entity<Tag>().HasIndex(t => t.IsActive);

            // === RELACIÓN PRODUCTO-TAG ===
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

            // Configuraciones de precios
            builder.Entity<Product>()
                .Property(p => p.BasePrice)
                .HasColumnType("numeric(10,2)");

            // Configuraciones de strings
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

            // Configuración de fechas con valores por defecto
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
        }
    }
}
