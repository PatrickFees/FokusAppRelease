using System;
using FokusApp.Core.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{
    [DbContext(typeof(FokusAppDbContext))]
    [Migration("20260701000100_MoveFocusTimerMinutesToTasks")]
    public partial class MoveFocusTimerMinutesToTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimerMinutes",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.Sql(
                """
                UPDATE Tasks
                SET TimerMinutes = (
                    SELECT CAST(strftime('%H', FocusTimers.TimeSet) AS INTEGER) * 60
                         + CAST(strftime('%M', FocusTimers.TimeSet) AS INTEGER)
                    FROM FocusTimers
                    WHERE FocusTimers.TaskId = Tasks.Id
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM FocusTimers
                    WHERE FocusTimers.TaskId = Tasks.Id
                );
                """);

            migrationBuilder.DropTable(name: "FocusTimers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FocusTimers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TimeSet = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusTimers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FocusTimers_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO FocusTimers (Id, TaskId, TimeSet)
                SELECT lower(hex(randomblob(4))) || '-' ||
                       lower(hex(randomblob(2))) || '-' ||
                       '4' || substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       lower(hex(randomblob(6))),
                       Id,
                       datetime('now', 'start of day', '+' || TimerMinutes || ' minutes')
                FROM Tasks
                WHERE TimerMinutes > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FocusTimers_TaskId",
                table: "FocusTimers",
                column: "TaskId",
                unique: true);

            migrationBuilder.DropColumn(
                name: "TimerMinutes",
                table: "Tasks");
        }
    }
}
