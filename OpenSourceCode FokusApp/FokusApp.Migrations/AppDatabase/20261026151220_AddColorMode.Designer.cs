
using System;
using FokusApp.Core.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{
    [DbContext(typeof(FokusAppDbContext))]
    [Migration("20261026151220_AddColorMode")]
    partial class AddColorMode
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "10.0.10");

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.ForYouSuggestionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PoolKey")
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ForYouSuggestions");
                });

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

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.SettingEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("BalanceGoal")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ColorMode")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("CompanionEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CompanionHiddenDay")
                        .HasColumnType("TEXT");

                    b.Property<int>("CustomForYou")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CustomToDo")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DefaultScope")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Language")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("MicroFeedbackEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OnboardingState")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ReviewsEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("RingtoneId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Theme")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("RingtoneId");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("FokusApp.Core.DataBase.Entities.TaskEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("BreakMinutes")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDone")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsPositive")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTemplate")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsWorkTask")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Note")
                        .HasColumnType("TEXT");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("RecurrenceGeneratedUntil")
                        .HasColumnType("TEXT");

                    b.Property<int>("RecurrenceInterval")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RecurrenceUnit")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RecurrenceWeekdays")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ReminderMinutesBefore")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("SeriesId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("TaskDate")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan?>("TaskTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("TimerMinutes")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Tasks", t =>
                        {
                            t.HasCheckConstraint("CK_Tasks_TemplateHasNoDate", "IsTemplate = 0 OR TaskDate IS NULL");
                        });
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
