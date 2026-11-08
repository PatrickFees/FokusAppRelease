using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddReminderMode : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
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
                defaultValue: 1200);

            migrationBuilder.AddColumn<int>(
                name: "ReminderMode",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
