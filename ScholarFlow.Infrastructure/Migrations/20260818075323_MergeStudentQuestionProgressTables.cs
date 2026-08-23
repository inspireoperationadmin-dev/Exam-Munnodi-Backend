using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeStudentQuestionProgressTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentQuestionHistories");

            migrationBuilder.DropTable(
                name: "StudentTopicQuestionProgresses");

            migrationBuilder.CreateTable(
                name: "StudentQuestionProgresses",
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
                    LastResponseWasAnswered = table.Column<bool>(type: "bit", nullable: false),
                    ConsecutiveCorrect = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveWrong = table.Column<int>(type: "int", nullable: false),
                    MasteryScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttemptMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuestionProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentQuestionProgresses_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentQuestionProgresses_SubTopics_SubTopicId",
                        column: x => x.SubTopicId,
                        principalTable: "SubTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_QuestionId",
                table: "StudentQuestionProgresses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_SubTopicId",
                table: "StudentQuestionProgresses",
                column: "SubTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_UserId_LastAnswerCorrect",
                table: "StudentQuestionProgresses",
                columns: new[] { "UserId", "LastAnswerCorrect" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_UserId_LastSeenAt",
                table: "StudentQuestionProgresses",
                columns: new[] { "UserId", "LastSeenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_UserId_QuestionId",
                table: "StudentQuestionProgresses",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_UserId_SubjectId_TopicId",
                table: "StudentQuestionProgresses",
                columns: new[] { "UserId", "SubjectId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionProgresses_UserId_SubTopicId_Status",
                table: "StudentQuestionProgresses",
                columns: new[] { "UserId", "SubTopicId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentQuestionProgresses");

            migrationBuilder.CreateTable(
                name: "StudentQuestionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    LastAnswerCorrect = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimesAttempted = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentQuestionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentQuestionHistories_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentTopicQuestionProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    LastAnswerCorrect = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesAttempted = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "IX_StudentQuestionHistories_QuestionId",
                table: "StudentQuestionHistories",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionHistories_UserId_LastAnswerCorrect",
                table: "StudentQuestionHistories",
                columns: new[] { "UserId", "LastAnswerCorrect" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuestionHistories_UserId_QuestionId",
                table: "StudentQuestionHistories",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

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
    }
}
