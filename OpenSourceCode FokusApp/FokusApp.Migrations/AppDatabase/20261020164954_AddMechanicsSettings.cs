using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddMechanicsSettings : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BalanceGoal",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CompanionEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MicroFeedbackEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RemindersEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReviewsEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceGoal",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CompanionEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "MicroFeedbackEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "RemindersEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ReviewsEnabled",
                table: "Settings");
        }
    }
}
