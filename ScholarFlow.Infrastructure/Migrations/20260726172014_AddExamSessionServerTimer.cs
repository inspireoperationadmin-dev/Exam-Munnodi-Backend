using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionServerTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                table: "ExamSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_ExpiresAt",
                table: "ExamSessions",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_ExpiresAt",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                table: "ExamSessions");
        }
    }
}
