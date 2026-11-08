using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class AddCustomBalanceRatio : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomForYou",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CustomToDo",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE \"Settings\" SET \"BalanceGoal\" = 6, \"CustomForYou\" = 1, \"CustomToDo\" = 2 WHERE \"BalanceGoal\" = 1;");
            migrationBuilder.Sql("UPDATE \"Settings\" SET \"BalanceGoal\" = 6, \"CustomForYou\" = 1, \"CustomToDo\" = 3 WHERE \"BalanceGoal\" = 2;");
            migrationBuilder.Sql("UPDATE \"Settings\" SET \"BalanceGoal\" = 0 WHERE \"BalanceGoal\" = 3;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomForYou",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CustomToDo",
                table: "Settings");
        }
    }
}
