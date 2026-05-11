using BenServicesPlatform.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProviderEntity> Providers => Set<ProviderEntity>();
    public DbSet<ProviderApplicationEntity> ProviderApplications => Set<ProviderApplicationEntity>();
    public DbSet<AdminEntity> Admins => Set<AdminEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdminEntity>(entity =>
        {
            entity.ToTable("admins");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedOnAdd();
            entity.Property(item => item.FullName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Username).HasMaxLength(120).IsRequired();
            entity.Property(item => item.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(item => item.Role).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(item => item.UpdatedAt).HasColumnType("datetime(6)");

            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => item.Role);
            entity.HasIndex(item => item.IsActive);

            entity.HasOne(item => item.CreatedByAdmin)
                .WithMany(item => item.CreatedAdmins)
                .HasForeignKey(item => item.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProviderEntity>(entity =>
        {
            entity.ToTable("providers");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedOnAdd();
            entity.Property(item => item.FullName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.BusinessName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Phone).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ServiceType).HasMaxLength(24).IsRequired();
            entity.Property(item => item.ServicesOfferedJson).HasColumnType("longtext").IsRequired();
            entity.Property(item => item.City).HasMaxLength(120).IsRequired();
            entity.Property(item => item.State).HasMaxLength(16).IsRequired();
            entity.Property(item => item.ZipCodesJson).HasColumnType("longtext").IsRequired();
            entity.Property(item => item.Region).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Availability).HasMaxLength(80).IsRequired();
            entity.Property(item => item.WorkingHours).HasMaxLength(80).IsRequired();
            entity.Property(item => item.VerificationStatus).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Source).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(2000);
            entity.Property(item => item.AdminComments).HasMaxLength(2000);
            entity.Property(item => item.W9FilePath).HasMaxLength(500);
            entity.Property(item => item.CoiFilePath).HasMaxLength(500);
            entity.Property(item => item.W9UploadedAt).HasColumnType("datetime(6)");
            entity.Property(item => item.CoiUploadedAt).HasColumnType("datetime(6)");
            entity.Property(item => item.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(item => item.UpdatedAt).HasColumnType("datetime(6)");
            entity.Property(item => item.VerifiedAt).HasColumnType("datetime(6)");

            entity.HasIndex(item => item.City);
            entity.HasIndex(item => item.State);
            entity.HasIndex(item => item.Region);
            entity.HasIndex(item => item.IsActive);
            entity.HasIndex(item => item.VerificationStatus);
        });

        modelBuilder.Entity<ProviderApplicationEntity>(entity =>
        {
            entity.ToTable("provider_applications");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedOnAdd();
            entity.Property(item => item.FullName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.BusinessName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Phone).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ServiceType).HasMaxLength(24).IsRequired();
            entity.Property(item => item.ServicesOfferedJson).HasColumnType("longtext").IsRequired();
            entity.Property(item => item.CitiesCoveredJson).HasColumnType("longtext").IsRequired();
            entity.Property(item => item.City).HasMaxLength(120).IsRequired();
            entity.Property(item => item.State).HasMaxLength(16).IsRequired();
            entity.Property(item => item.ZipCodesJson).HasColumnType("longtext").IsRequired();
            entity.Property(item => item.WorkingHours).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Message).HasMaxLength(2000).IsRequired();
            entity.Property(item => item.Source).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();
            entity.Property(item => item.SubmittedAt).HasColumnType("datetime(6)");
            entity.Property(item => item.LicenseFileName).HasMaxLength(255);

            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.City);
            entity.HasIndex(item => item.State);
            entity.HasIndex(item => item.SubmittedAt);
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AdminEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
