using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddColorMode : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColorMode",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Settings\" SET \"ColorMode\" = 1, \"Theme\" = 1 WHERE \"Theme\" = 2;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Settings\" SET \"Theme\" = 2 WHERE \"ColorMode\" = 1;");

            migrationBuilder.DropColumn(
                name: "ColorMode",
                table: "Settings");
        }
    }
}
