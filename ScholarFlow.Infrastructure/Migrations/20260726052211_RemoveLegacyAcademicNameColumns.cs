using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyAcademicNameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Streams]
                SET [NameEnglish] = [Name]
                WHERE ([NameEnglish] IS NULL OR [NameEnglish] = N'')
                  AND [Name] IS NOT NULL
                """);

            migrationBuilder.Sql("""
                UPDATE [Subjects]
                SET [NameEnglish] = [Name]
                WHERE ([NameEnglish] IS NULL OR [NameEnglish] = N'')
                  AND [Name] IS NOT NULL
                """);

            migrationBuilder.Sql("""
                UPDATE [Topics]
                SET [NameEnglish] = [TopicName]
                WHERE ([NameEnglish] IS NULL OR [NameEnglish] = N'')
                  AND [TopicName] IS NOT NULL
                """);

            migrationBuilder.Sql("""
                UPDATE [SubTopics]
                SET [NameEnglish] = [SubTopicName]
                WHERE ([NameEnglish] IS NULL OR [NameEnglish] = N'')
                  AND [SubTopicName] IS NOT NULL
                """);

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Streams_Name",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "TopicName",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "SubTopicName",
                table: "SubTopics");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Streams");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_NameEnglish",
                table: "Subjects",
                column: "NameEnglish",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Streams_NameEnglish",
                table: "Streams",
                column: "NameEnglish",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_NameEnglish",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Streams_NameEnglish",
                table: "Streams");

            migrationBuilder.AddColumn<string>(
                name: "TopicName",
                table: "Topics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubTopicName",
                table: "SubTopics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Streams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [Streams]
                SET [Name] = [NameEnglish]
                WHERE [Name] = N''
                """);

            migrationBuilder.Sql("""
                UPDATE [Subjects]
                SET [Name] = [NameEnglish]
                WHERE [Name] = N''
                """);

            migrationBuilder.Sql("""
                UPDATE [Topics]
                SET [TopicName] = [NameEnglish]
                WHERE [TopicName] = N''
                """);

            migrationBuilder.Sql("""
                UPDATE [SubTopics]
                SET [SubTopicName] = [NameEnglish]
                WHERE [SubTopicName] = N''
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Streams_Name",
                table: "Streams",
                column: "Name",
                unique: true);
        }
    }
}
