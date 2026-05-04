using Microsoft.EntityFrameworkCore;
using SideCar.Business.Entities;
using SideCar.Business.Enums;

namespace SideCar.Business.Data
{
    public class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Id))
                    .ValueGeneratedOnAdd();

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");
            }

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role)
                    .HasMaxLength(50)
                    .HasDefaultValue(Roles.User)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<UserActivityLog>(entity =>
            {
                entity.Property(e => e.ActivityType).HasConversion<string>();
            });
        }
    }
}
