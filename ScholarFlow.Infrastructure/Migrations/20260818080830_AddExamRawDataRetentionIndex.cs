using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamRawDataRetentionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_EndTime_Status",
                table: "ExamSessions",
                columns: new[] { "EndTime", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_EndTime_Status",
                table: "ExamSessions");
        }
    }
}
