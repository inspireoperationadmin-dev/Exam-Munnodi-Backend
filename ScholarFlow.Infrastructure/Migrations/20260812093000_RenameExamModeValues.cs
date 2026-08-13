using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameExamModeValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ExamSessions
                SET Mode = CASE
                    WHEN Mode = 'Practice' AND PaperId IS NULL THEN 'TopicPractice'
                    WHEN Mode = 'Practice' THEN 'PaperPractice'
                    WHEN Mode = 'FixedExam' AND PaperId IS NULL THEN 'TopicExam'
                    WHEN Mode = 'FixedExam' THEN 'PaperExam'
                    ELSE Mode
                END
                WHERE Mode IN ('Practice', 'FixedExam')
                """);

            migrationBuilder.Sql("""
                UPDATE activity
                SET activity.Mode = session.Mode
                FROM StudentStudyActivities activity
                INNER JOIN ExamSessions session ON session.Id = activity.SessionId
                WHERE activity.Mode IN ('Practice', 'FixedExam')
                """);

            migrationBuilder.Sql("""
                UPDATE StudentStudyActivities
                SET Mode = CASE
                    WHEN Mode = 'Practice' THEN 'PaperPractice'
                    WHEN Mode = 'FixedExam' THEN 'PaperExam'
                    ELSE Mode
                END
                WHERE Mode IN ('Practice', 'FixedExam')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ExamSessions
                SET Mode = CASE
                    WHEN Mode IN ('PaperPractice', 'TopicPractice') THEN 'Practice'
                    WHEN Mode IN ('PaperExam', 'TopicExam') THEN 'FixedExam'
                    ELSE Mode
                END
                WHERE Mode IN ('PaperPractice', 'TopicPractice', 'PaperExam', 'TopicExam')
                """);

            migrationBuilder.Sql("""
                UPDATE StudentStudyActivities
                SET Mode = CASE
                    WHEN Mode IN ('PaperPractice', 'TopicPractice') THEN 'Practice'
                    WHEN Mode IN ('PaperExam', 'TopicExam') THEN 'FixedExam'
                    ELSE Mode
                END
                WHERE Mode IN ('PaperPractice', 'TopicPractice', 'PaperExam', 'TopicExam')
                """);
        }
    }
}
