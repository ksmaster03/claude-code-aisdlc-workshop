using Microsoft.EntityFrameworkCore;
using OpsTrack.Data.Entities;

namespace OpsTrack.Data;

public class OpsTrackDbContext : DbContext
{
    public OpsTrackDbContext(DbContextOptions<OpsTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<Line> Lines { get; set; }

    public DbSet<Equipment> Equipment { get; set; }

    public DbSet<WorkRequest> WorkRequests { get; set; }

    public DbSet<RunningNumber> RunningNumbers { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Line>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Equipment>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.Line).WithMany(l => l.Equipment).HasForeignKey(x => x.LineId);
        });

        modelBuilder.Entity<WorkRequest>(e =>
        {
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ReportedBy).HasMaxLength(50).IsRequired();
            e.Property(x => x.AssignedTo).HasMaxLength(50);
            e.Property(x => x.ClosedBy).HasMaxLength(50);
            e.HasIndex(x => x.Number);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId);
        });

        modelBuilder.Entity<RunningNumber>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(20);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Entity).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Action).HasMaxLength(30).IsRequired();
            e.Property(x => x.By).HasMaxLength(50).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(1000);
        });
    }
}
