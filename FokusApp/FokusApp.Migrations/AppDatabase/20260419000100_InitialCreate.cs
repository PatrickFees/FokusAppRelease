using System;
using FokusApp.Core.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.AppDatabase
{
    [DbContext(typeof(FokusAppDbContext))]
    [Migration("20260419000100_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    ExceptionType = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    InnerException = table.Column<string>(type: "TEXT", nullable: true),
                    Route = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", nullable: true),
                    OsVersion = table.Column<string>(type: "TEXT", nullable: true),
                    AppVersion = table.Column<string>(type: "TEXT", nullable: true),
                    IsFatal = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUiFailure = table.Column<bool>(type: "INTEGER", nullable: false),
                    ThreadId = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ringtones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RingtoneFilePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ringtones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    IsRegularTask = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWorkTask = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsPositive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TaskDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    BreakMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Theme = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<int>(type: "INTEGER", nullable: false),
                    RingtoneId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Ringtones_RingtoneId",
                        column: x => x.RingtoneId,
                        principalTable: "Ringtones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FocusTimers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeSet = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_AppErrorLogs_OccurredAtUtc",
                table: "AppErrorLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FocusTimers_TaskId",
                table: "FocusTimers",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RingtoneId",
                table: "Users",
                column: "RingtoneId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppErrorLogs");
            migrationBuilder.DropTable(name: "FocusTimers");
            migrationBuilder.DropTable(name: "Users");
            migrationBuilder.DropTable(name: "Tasks");
            migrationBuilder.DropTable(name: "Ringtones");
        }
    }
}
