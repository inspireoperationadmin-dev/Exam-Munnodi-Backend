using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ManageSubscriptionPricingAndDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasePriceLkr",
                table: "SubscriptionPlans",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "SubscriptionPlans",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE SubscriptionPlans
                SET BasePriceLkr = PriceLkr,
                    DiscountPercentage = 0;

                UPDATE SubscriptionPlans
                SET BasePriceLkr = ROUND(PriceLkr / 0.70, 2),
                    DiscountPercentage = 30
                WHERE BillingCycle = N'Annual'
                  AND PriceLkr > 0;
                """);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanPriceChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousBasePriceLkr = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PreviousDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PreviousPriceLkr = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    NewBasePriceLkr = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    NewDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NewPriceLkr = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ChangedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanPriceChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanPriceChanges_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPriceChanges_CreatedAt",
                table: "SubscriptionPlanPriceChanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPriceChanges_PlanId",
                table: "SubscriptionPlanPriceChanges",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlanPriceChanges");

            migrationBuilder.DropColumn(
                name: "BasePriceLkr",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "SubscriptionPlans");
        }
    }
}
