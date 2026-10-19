using System;
using FokusApp.Core.DataBase.ErrorLogging;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FokusApp.Migrations.ErrorDatabase
{
    [DbContext(typeof(AppErrorDbContext))]
    [Migration("20260709000100_InitialErrorDatabaseCreate")]
    public partial class InitialErrorDatabaseCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "AppErrorLogs" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_AppErrorLogs" PRIMARY KEY,
                    "OccurredAtUtc" TEXT NOT NULL,
                    "Source" TEXT NOT NULL,
                    "ExceptionType" TEXT NOT NULL,
                    "Message" TEXT NOT NULL,
                    "StackTrace" TEXT NULL,
                    "InnerException" TEXT NULL,
                    "Route" TEXT NULL,
                    "Platform" TEXT NULL,
                    "OsVersion" TEXT NULL,
                    "AppVersion" TEXT NULL,
                    "IsFatal" INTEGER NOT NULL,
                    "IsUiFailure" INTEGER NOT NULL,
                    "ThreadId" INTEGER NOT NULL,
                    "PayloadJson" TEXT NULL
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AppErrorLogs_OccurredAtUtc"
                ON "AppErrorLogs" ("OccurredAtUtc");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppErrorLogs");
        }
    }
}
