using System;
using FokusApp.Core.DataBase.ErrorLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FokusApp.Migrations.ErrorDatabase
{
    [DbContext(typeof(AppErrorDbContext))]
    partial class AppErrorDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.10");

            modelBuilder.Entity("FokusApp.Core.DataBase.ErrorLogging.Entities.AppErrorLogEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AppVersion")
                        .HasColumnType("TEXT");

                    b.Property<string>("ExceptionType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsFatal")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsUiFailure")
                        .HasColumnType("INTEGER");

                    b.Property<string>("InnerException")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("OccurredAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("OsVersion")
                        .HasColumnType("TEXT");

                    b.Property<string>("PayloadJson")
                        .HasColumnType("TEXT");

                    b.Property<string>("Platform")
                        .HasColumnType("TEXT");

                    b.Property<string>("Route")
                        .HasColumnType("TEXT");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StackTrace")
                        .HasColumnType("TEXT");

                    b.Property<int>("ThreadId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OccurredAtUtc");

                    b.ToTable("AppErrorLogs");
                });
#pragma warning restore 612, 618
        }
    }
}
