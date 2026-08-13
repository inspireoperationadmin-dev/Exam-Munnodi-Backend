using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicQuestionProgressAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CoveragePercentage",
                table: "StudentSubTopicPerformances",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HealthPercentage",
                table: "StudentSubTopicPerformances",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MasteredQuestions",
                table: "StudentSubTopicPerformances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MasteryPercentage",
                table: "StudentSubTopicPerformances",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestionsInSubTopic",
                table: "StudentSubTopicPerformances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UniqueQuestionsAttempted",
                table: "StudentSubTopicPerformances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudentTopicQuestionProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesAttempted = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    LastAnswerCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTopicQuestionProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentTopicQuestionProgresses_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentTopicQuestionProgresses_SubTopics_SubTopicId",
                        column: x => x.SubTopicId,
                        principalTable: "SubTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubTopicPerformances_UserId_SubjectId_HealthPercentage",
                table: "StudentSubTopicPerformances",
                columns: new[] { "UserId", "SubjectId", "HealthPercentage" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentTopicQuestionProgresses_QuestionId",
                table: "StudentTopicQuestionProgresses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTopicQuestionProgresses_SubTopicId",
                table: "StudentTopicQuestionProgresses",
                column: "SubTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTopicQuestionProgresses_UserId_QuestionId",
                table: "StudentTopicQuestionProgresses",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentTopicQuestionProgresses_UserId_SubjectId_TopicId",
                table: "StudentTopicQuestionProgresses",
                columns: new[] { "UserId", "SubjectId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentTopicQuestionProgresses_UserId_SubTopicId_Status",
                table: "StudentTopicQuestionProgresses",
                columns: new[] { "UserId", "SubTopicId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentTopicQuestionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_StudentSubTopicPerformances_UserId_SubjectId_HealthPercentage",
                table: "StudentSubTopicPerformances");

            migrationBuilder.DropColumn(
                name: "CoveragePercentage",
                table: "StudentSubTopicPerformances");

            migrationBuilder.DropColumn(
                name: "HealthPercentage",
                table: "StudentSubTopicPerformances");

            migrationBuilder.DropColumn(
                name: "MasteredQuestions",
                table: "StudentSubTopicPerformances");

            migrationBuilder.DropColumn(
                name: "MasteryPercentage",
                table: "StudentSubTopicPerformances");

            migrationBuilder.DropColumn(
                name: "TotalQuestionsInSubTopic",
                table: "StudentSubTopicPerformances");

            migrationBuilder.DropColumn(
                name: "UniqueQuestionsAttempted",
                table: "StudentSubTopicPerformances");
        }
    }
}
