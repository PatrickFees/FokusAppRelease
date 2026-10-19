using System;
using FokusApp.Core.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{
    [DbContext(typeof(FokusAppDbContext))]
    [Migration("20260721000100_EnforceUndatedRegularTasks")]
    public partial class EnforceUndatedRegularTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Tasks
                SET TaskDate = NULL
                WHERE IsRegularTask = 1 AND TaskDate IS NOT NULL;
                """);

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "TasksWithoutRegularTaskDateConstraint");

            CreateTasksTable(migrationBuilder, includeRegularTaskDateConstraint: true);
            CopyTasks(migrationBuilder);

            migrationBuilder.DropTable(name: "TasksWithoutRegularTaskDateConstraint");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "TasksWithoutRegularTaskDateConstraint");

            CreateTasksTable(migrationBuilder, includeRegularTaskDateConstraint: false);
            CopyTasks(migrationBuilder);

            migrationBuilder.DropTable(name: "TasksWithoutRegularTaskDateConstraint");
        }

        private static void CreateTasksTable(
            MigrationBuilder migrationBuilder,
            bool includeRegularTaskDateConstraint)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    IsRegularTask = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWorkTask = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: true),
                    IsPositive = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TaskDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    BreakMinutes = table.Column<int>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 5),
                    TimerMinutes = table.Column<int>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: 5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", task => task.Id);

                    if (includeRegularTaskDateConstraint)
                    {
                        table.CheckConstraint(
                            "CK_Tasks_RegularTaskHasNoDate",
                            "IsRegularTask = 0 OR TaskDate IS NULL");
                    }
                });
        }

        private static void CopyTasks(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO Tasks (
                    Id,
                    IsDone,
                    Note,
                    IsRegularTask,
                    IsWorkTask,
                    IsPositive,
                    CreationDate,
                    TaskDate,
                    Priority,
                    BreakMinutes,
                    TimerMinutes)
                SELECT Id,
                       IsDone,
                       Note,
                       IsRegularTask,
                       IsWorkTask,
                       IsPositive,
                       CreationDate,
                       TaskDate,
                       Priority,
                       BreakMinutes,
                       TimerMinutes
                FROM TasksWithoutRegularTaskDateConstraint;
                """);
        }
    }
}
