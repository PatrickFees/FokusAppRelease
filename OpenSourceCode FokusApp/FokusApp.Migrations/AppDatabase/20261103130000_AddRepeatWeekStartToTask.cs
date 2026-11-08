using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddRepeatWeekStartToTask : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepeatWeekStart",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepeatWeekStart",
                table: "Tasks");
        }
    }
}
