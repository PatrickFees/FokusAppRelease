using System;
using FokusApp.Core.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{
    [DbContext(typeof(FokusAppDbContext))]
    partial class FokusAppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.10");

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.RingtonesEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("RingtoneFilePath")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Ringtones");
                });

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.TaskEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("BreakMinutes")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDone")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsPositive")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRegularTask")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsWorkTask")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Note")
                        .HasColumnType("TEXT");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("TaskDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("TimerMinutes")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Tasks", t =>
                        {
                            t.HasCheckConstraint("CK_Tasks_RegularTaskHasNoDate", "IsRegularTask = 0 OR TaskDate IS NULL");
                        });
                });

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.SettingEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Language")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("RingtoneId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Theme")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("RingtoneId");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.SettingEntity", b =>
                {
                    b.HasOne("FokusApp.Core.DataBase.Entities.RingtonesEntity", "Ringtone")
                        .WithMany()
                        .HasForeignKey("RingtoneId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Ringtone");
                });
#pragma warning restore 612, 618
        }
    }
}
