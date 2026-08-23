using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ScholarFlow.Infrastructure.Persistence;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260822115000_EnforceExamSessionStartPolicy")]
public sealed class EnforceExamSessionStartPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "TopicId",
            table: "ExamSessions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExamSessions_TopicId",
            table: "ExamSessions",
            column: "TopicId");

        migrationBuilder.CreateIndex(
            name: "IX_ExamSessions_UserId_PaperId_Mode_Status",
            table: "ExamSessions",
            columns: ["UserId", "PaperId", "Mode", "Status"]);

        migrationBuilder.CreateIndex(
            name: "IX_ExamSessions_UserId_TopicId_Mode_Status",
            table: "ExamSessions",
            columns: ["UserId", "TopicId", "Mode", "Status"]);

        migrationBuilder.AddForeignKey(
            name: "FK_ExamSessions_Topics_TopicId",
            table: "ExamSessions",
            column: "TopicId",
            principalTable: "Topics",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ExamSessions_Topics_TopicId",
            table: "ExamSessions");

        migrationBuilder.DropIndex(
            name: "IX_ExamSessions_TopicId",
            table: "ExamSessions");

        migrationBuilder.DropIndex(
            name: "IX_ExamSessions_UserId_PaperId_Mode_Status",
            table: "ExamSessions");

        migrationBuilder.DropIndex(
            name: "IX_ExamSessions_UserId_TopicId_Mode_Status",
            table: "ExamSessions");

        migrationBuilder.DropColumn(
            name: "TopicId",
            table: "ExamSessions");
    }
}
