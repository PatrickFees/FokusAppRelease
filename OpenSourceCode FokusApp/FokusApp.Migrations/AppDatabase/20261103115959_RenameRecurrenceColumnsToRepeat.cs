using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class RenameRecurrenceColumnsToRepeat : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecurrenceGeneratedUntil",
                table: "Tasks",
                newName: "RepeatGeneratedUntil");

            migrationBuilder.RenameColumn(
                name: "RecurrenceInterval",
                table: "Tasks",
                newName: "RepeatInterval");

            migrationBuilder.RenameColumn(
                name: "RecurrenceUnit",
                table: "Tasks",
                newName: "RepeatUnit");

            migrationBuilder.RenameColumn(
                name: "RecurrenceWeekdays",
                table: "Tasks",
                newName: "RepeatWeekdays");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RepeatGeneratedUntil",
                table: "Tasks",
                newName: "RecurrenceGeneratedUntil");

            migrationBuilder.RenameColumn(
                name: "RepeatInterval",
                table: "Tasks",
                newName: "RecurrenceInterval");

            migrationBuilder.RenameColumn(
                name: "RepeatUnit",
                table: "Tasks",
                newName: "RecurrenceUnit");

            migrationBuilder.RenameColumn(
                name: "RepeatWeekdays",
                table: "Tasks",
                newName: "RecurrenceWeekdays");
        }
    }
}
