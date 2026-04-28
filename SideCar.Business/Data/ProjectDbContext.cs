using Microsoft.EntityFrameworkCore;
using SideCar.Business.Entities;
using SideCar.Business.Enums;

namespace SideCar.Business.Data
{
    public class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
    {
        public DbSet<Users> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Id)
                        .HasDefaultValueSql("NEWSEQUENTIALID()");
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role)
                    .HasMaxLength(50)
                    .HasDefaultValue(Roles.User)
                    .HasConversion<string>();
            });

        }
    }
}
