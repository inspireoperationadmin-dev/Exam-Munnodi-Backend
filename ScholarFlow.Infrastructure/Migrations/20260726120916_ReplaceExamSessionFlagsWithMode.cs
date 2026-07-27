using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceExamSessionFlagsWithMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "ExamSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Practice");

            migrationBuilder.Sql("""
                UPDATE ExamSessions
                SET Mode = CASE
                    WHEN IsPersonalized = 1 THEN 'MockExam'
                    WHEN IsPractice = 1 THEN 'Practice'
                    ELSE 'FixedExam'
                END
                """);

            migrationBuilder.DropColumn(
                name: "IsPersonalized",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "IsPractice",
                table: "ExamSessions");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_Mode",
                table: "ExamSessions",
                column: "Mode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPersonalized",
                table: "ExamSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPractice",
                table: "ExamSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE ExamSessions
                SET
                    IsPractice = CASE WHEN Mode = 'Practice' THEN 1 ELSE 0 END,
                    IsPersonalized = CASE WHEN Mode = 'MockExam' THEN 1 ELSE 0 END
                """);

            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_Mode",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ExamSessions");
        }
    }
}
