using FokusApp.Core.DataBase.ErrorLogging.Entities;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.ErrorLogging;

public class AppErrorDbContext : DbContext
{
    public AppErrorDbContext(DbContextOptions<AppErrorDbContext> options) : base(options)
    {
    }

    public DbSet<AppErrorLogEntity> AppErrorLogs => Set<AppErrorLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppErrorLogEntity>()
            .Property(errorLog => errorLog.Source)
            .IsRequired();

        modelBuilder.Entity<AppErrorLogEntity>()
            .Property(errorLog => errorLog.ExceptionType)
            .IsRequired();

        modelBuilder.Entity<AppErrorLogEntity>()
            .Property(errorLog => errorLog.Message)
            .IsRequired();

        modelBuilder.Entity<AppErrorLogEntity>()
            .HasIndex(errorLog => errorLog.OccurredAtUtc);
    }
}
