using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddOnboardingState : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnboardingState",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Settings\" SET \"OnboardingState\" = 2;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingState",
                table: "Settings");
        }
    }
}
