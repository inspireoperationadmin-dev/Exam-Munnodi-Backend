using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrectSubscriptionPlansAndEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsPaperExamMode",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "SubscriptionPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<int>(
                name: "FreeModelPaperCount",
                table: "SubscriptionPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FreePastPaperCount",
                table: "SubscriptionPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyMockExamLimit",
                table: "SubscriptionPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyUnitExamLimit",
                table: "SubscriptionPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgressAccessLevel",
                table: "SubscriptionPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Overall");

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "SubscriptionPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<string>(
                name: "ActivationSource",
                table: "StudentSubscriptions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AdminManual");

            migrationBuilder.AddColumn<string>(
                name: "PromotionCode",
                table: "StudentSubscriptions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentSubscriptionUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "date", nullable: false),
                    Feature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSubscriptionUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSubscriptionUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubscriptions_UserId_PromotionCode",
                table: "StudentSubscriptions",
                columns: new[] { "UserId", "PromotionCode" },
                unique: true,
                filter: "[PromotionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubscriptionUsages_UserId_PeriodStart_Feature",
                table: "StudentSubscriptionUsages",
                columns: new[] { "UserId", "PeriodStart", "Feature" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentSubscriptionUsages");

            migrationBuilder.DropIndex(
                name: "IX_StudentSubscriptions_UserId_PromotionCode",
                table: "StudentSubscriptions");

            migrationBuilder.DropColumn(
                name: "AllowsPaperExamMode",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "FreeModelPaperCount",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "FreePastPaperCount",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MonthlyMockExamLimit",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MonthlyUnitExamLimit",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ProgressAccessLevel",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ActivationSource",
                table: "StudentSubscriptions");

            migrationBuilder.DropColumn(
                name: "PromotionCode",
                table: "StudentSubscriptions");
        }
    }
}
