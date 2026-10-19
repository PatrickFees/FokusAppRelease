using FokusApp.Core.DataBase.Entities;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase;

public class FokusAppDbContext : DbContext
{
    public FokusAppDbContext(DbContextOptions<FokusAppDbContext> options) : base(options)
    {
    }

    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<RingtonesEntity> Ringtones => Set<RingtonesEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SettingEntity>()
            .HasOne(setting => setting.Ringtone)
            .WithMany()
            .HasForeignKey(setting => setting.RingtoneId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.BreakMinutes)
            .ValueGeneratedNever();

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.TimerMinutes)
            .ValueGeneratedNever();

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.IsWorkTask)
            .ValueGeneratedNever();

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.IsPositive)
            .ValueGeneratedNever();

        modelBuilder.Entity<TaskEntity>()
            .ToTable(tableBuilder => tableBuilder.HasCheckConstraint(
                "CK_Tasks_RegularTaskHasNoDate",
                "IsRegularTask = 0 OR TaskDate IS NULL"));
    }
}
