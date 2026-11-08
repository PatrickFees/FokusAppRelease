using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddCalendarImportFieldsToTask : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepeatMonth",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatWeekdayOrdinal",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCalendarEventId",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepeatMonth",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RepeatWeekdayOrdinal",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SourceCalendarEventId",
                table: "Tasks");
        }
    }
}
