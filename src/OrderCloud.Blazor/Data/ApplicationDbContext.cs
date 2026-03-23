using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Blazor.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TenantDTO> Tenants { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TenantDTO>(entity =>
            {
                entity.ToTable("TenantDTO");  // match existing migration table name
                entity.HasKey(t => t.Id);
            });
        }
    }
}