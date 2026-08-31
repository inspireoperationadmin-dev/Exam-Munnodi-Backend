using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations;

public partial class AddOtpVerificationAttemptLimit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // OTP rows are temporary; discard malformed legacy values before indexing.
        migrationBuilder.Sql("DELETE FROM [OtpCodes] WHERE LEN([Email]) > 450;");

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "OtpCodes",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AddColumn<int>(
            name: "FailedAttemptCount",
            table: "OtpCodes",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_OtpCodes_Email_CreatedAt",
            table: "OtpCodes",
            columns: new[] { "Email", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OtpCodes_Email_CreatedAt",
            table: "OtpCodes");

        migrationBuilder.DropColumn(
            name: "FailedAttemptCount",
            table: "OtpCodes");

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "OtpCodes",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");
    }
}
