using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicMultilingualNamesSafe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameEnglish",
                table: "Topics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameSinhala",
                table: "Topics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameTamil",
                table: "Topics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEnglish",
                table: "SubTopics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameSinhala",
                table: "SubTopics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameTamil",
                table: "SubTopics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEnglish",
                table: "Subjects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameSinhala",
                table: "Subjects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameTamil",
                table: "Subjects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEnglish",
                table: "Streams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameSinhala",
                table: "Streams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameTamil",
                table: "Streams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Streams]
                SET [NameEnglish] = [Name]
                WHERE [NameEnglish] = N''
                """);

            migrationBuilder.Sql("""
                UPDATE [Subjects]
                SET [NameEnglish] = [Name]
                WHERE [NameEnglish] = N''
                """);

            migrationBuilder.Sql("""
                UPDATE [Topics]
                SET [NameEnglish] = [TopicName]
                WHERE [NameEnglish] = N''
                """);

            migrationBuilder.Sql("""
                UPDATE [SubTopics]
                SET [NameEnglish] = [SubTopicName]
                WHERE [NameEnglish] = N''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameEnglish",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "NameSinhala",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "NameTamil",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "NameEnglish",
                table: "SubTopics");

            migrationBuilder.DropColumn(
                name: "NameSinhala",
                table: "SubTopics");

            migrationBuilder.DropColumn(
                name: "NameTamil",
                table: "SubTopics");

            migrationBuilder.DropColumn(
                name: "NameEnglish",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "NameSinhala",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "NameTamil",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "NameEnglish",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "NameSinhala",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "NameTamil",
                table: "Streams");
        }
    }
}
