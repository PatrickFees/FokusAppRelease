using FokusApp.Core.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{
    [DbContext(typeof(FokusAppDbContext))]
    [Migration("20260714000100_RenameUsersToSettings")]
    public partial class RenameUsersToSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_Users_RingtoneId",
                table: "Settings");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_RingtoneId",
                table: "Settings",
                column: "RingtoneId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Settings_RingtoneId",
                table: "Settings");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RingtoneId",
                table: "Settings",
                column: "RingtoneId");

            migrationBuilder.RenameTable(
                name: "Settings",
                newName: "Users");
        }
    }
}
