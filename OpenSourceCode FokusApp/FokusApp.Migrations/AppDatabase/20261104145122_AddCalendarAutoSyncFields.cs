using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddCalendarAutoSyncFields : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSyncedNote",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncedReminderMinutesBefore",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LastSyncedTaskTime",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncedTimerMinutes",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CalendarAutoSyncEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalendarAutoSyncLastRunAt",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CalendarAutoSyncCalendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalendarId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarAutoSyncCalendars", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarAutoSyncCalendars");

            migrationBuilder.DropColumn(
                name: "LastSyncedNote",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastSyncedReminderMinutesBefore",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastSyncedTaskTime",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastSyncedTimerMinutes",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CalendarAutoSyncEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CalendarAutoSyncLastRunAt",
                table: "Settings");
        }
    }
}
