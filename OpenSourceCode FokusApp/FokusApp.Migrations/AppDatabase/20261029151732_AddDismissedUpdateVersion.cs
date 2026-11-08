using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddDismissedUpdateVersion : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DismissedUpdateVersion",
                table: "Settings",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DismissedUpdateVersion",
                table: "Settings");
        }
    }
}
