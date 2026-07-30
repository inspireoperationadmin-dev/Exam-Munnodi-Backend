using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    EndpointHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    P256dh = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Auth = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PushToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PushTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDevices_EndpointHash",
                table: "NotificationDevices",
                column: "EndpointHash",
                unique: true,
                filter: "[EndpointHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDevices_PushTokenHash",
                table: "NotificationDevices",
                column: "PushTokenHash",
                unique: true,
                filter: "[PushTokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDevices_UserId_Platform_Provider_IsActive",
                table: "NotificationDevices",
                columns: new[] { "UserId", "Platform", "Provider", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDevices");
        }
    }
}
