using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{

    public partial class RenameRegularTaskToTemplate : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_RegularTaskHasNoDate",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "IsRegularTask",
                table: "Tasks",
                newName: "IsTemplate");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_TemplateHasNoDate",
                table: "Tasks",
                sql: "IsTemplate = 0 OR TaskDate IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tasks_TemplateHasNoDate",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "IsTemplate",
                table: "Tasks",
                newName: "IsRegularTask");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tasks_RegularTaskHasNoDate",
                table: "Tasks",
                sql: "IsRegularTask = 0 OR TaskDate IS NULL");
        }
    }
}
