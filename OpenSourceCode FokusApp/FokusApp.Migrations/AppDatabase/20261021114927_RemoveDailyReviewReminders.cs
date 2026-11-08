using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class RemoveDailyReviewReminders : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderDay",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ReminderMinutes",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ReminderMode",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "RemindersEnabled",
                table: "Settings");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderDay",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderMinutes",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReminderMode",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RemindersEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
