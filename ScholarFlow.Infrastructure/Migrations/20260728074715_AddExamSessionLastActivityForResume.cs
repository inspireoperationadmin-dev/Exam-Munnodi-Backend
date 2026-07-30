using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionLastActivityForResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ExamSessions
                SET LastActivityAt = StartTime
                WHERE LastActivityAt IS NULL
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_LastActivityAt",
                table: "ExamSessions",
                column: "LastActivityAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_LastActivityAt",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "ExamSessions");
        }
    }
}
