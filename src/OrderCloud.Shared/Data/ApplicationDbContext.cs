using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Shared.Models;

namespace OrderCloud.Shared.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TenantDTO> Tenants { get; set; } = null!;
        public DbSet<CustomerDTO> Customers { get; set; } = null!;
        public DbSet<LocalUserDTO> LocalUsers { get; set; } = null!;
        public DbSet<DeviceDTO> Devices { get; set; } = null!;
        public DbSet<OrderDTO> Orders { get; set; } = null!;
        public DbSet<ItemDTO> Items { get; set; } = null!;
        public DbSet<CatalogItemDTO> CatalogItems { get; set; } = null!;
        public DbSet<BillDTO> Bills { get; set; } = null!;
        public DbSet<BillItemDTO> BillItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TenantDTO>(b =>
            {
                // Заменяем "TenantDTO" на "Tenants"
                b.ToTable("Tenants");
                b.HasKey(t => t.Id);
            });

            modelBuilder.Entity<CustomerDTO>(b =>
            {
                // Заменяем "CustomerDTO" на "Customers"
                b.ToTable("Customers");
                b.HasKey(c => c.Id);
            });

            modelBuilder.Entity<DeviceDTO>(b =>
            {
                // Заменяем "DeviceDTO" на "Devices"
                b.ToTable("Devices");
                b.HasKey(d => d.Id);
                b.HasOne(d => d.Tenant)
                    .WithMany()
                    .HasForeignKey(d => d.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LocalUserDTO>(b =>
            {
                // Заменяем "LocalUserDTO" на "LocalUsers"
                b.ToTable("LocalUsers");
                b.HasKey(u => u.Id);
                b.HasOne(u => u.Device)
                    .WithMany()
                    .HasForeignKey(u => u.DeviceId);
                b.HasOne(u => u.Tenant)
                    .WithMany()
                    .HasForeignKey(u => u.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CatalogItemDTO>(b =>
            {
                b.ToTable("CatalogItems");
                b.HasKey(i => i.Id);
                b.Property(i => i.Price).HasColumnType("decimal(18,2)");
                b.HasOne(i => i.Tenant)
                    .WithMany()
                    .HasForeignKey(i => i.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderDTO>(b =>
            {
                b.ToTable("Orders");
                b.HasKey(o => o.Id);
                b.Property(o => o.Status).IsRequired();
                b.Property(o => o.Total).HasColumnType("decimal(18,2)");
                
                // Явно указываем, что колонка может быть NULL в базе данных
                b.Property(o => o.CustomerId).IsRequired(false);
                b.Property(o => o.LocalUserId).IsRequired(false);

                b.HasOne(o => o.Tenant)
                    .WithMany()
                    .HasForeignKey(o => o.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(o => o.LocalUser)
                    .WithMany()
                    .HasForeignKey(o => o.LocalUserId);
                b.HasOne(o => o.Customer)
                    .WithMany()
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false); // Явно указывает, что связь необязательна
                b.HasMany(o => o.Items)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemDTO>(b =>
            {
                b.ToTable("ItemDTO");
                b.HasKey(i => i.Id);
                b.Property(i => i.Price).HasColumnType("decimal(18,2)");
                b.Property(i => i.Quantity).HasColumnType("decimal(18,2)");
                b.Property(i => i.Total).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<BillDTO>(b =>
            {
                b.ToTable("Bills");
                b.HasKey(bill => bill.Id);
                b.Property(bill => bill.PaymentMethod).IsRequired();
                b.Property(bill => bill.Subtotal).HasColumnType("decimal(18,2)");
                b.Property(bill => bill.Total).HasColumnType("decimal(18,2)");
                b.HasOne(bill => bill.Tenant)
                    .WithMany()
                    .HasForeignKey(bill => bill.TenantId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                b.HasMany(bill => bill.Items)
                    .WithOne(i => i.Bill)
                    .HasForeignKey(i => i.BillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BillItemDTO>(b =>
            {
                b.ToTable("BillItems");
                b.HasKey(i => i.Id);
                b.Property(i => i.Price).HasColumnType("decimal(18,2)");
                b.Property(i => i.Quantity).HasColumnType("decimal(18,2)");
                b.Property(i => i.Total).HasColumnType("decimal(18,2)");
            });
        }
    }
}

