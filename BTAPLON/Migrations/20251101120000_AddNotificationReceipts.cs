using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTAPLON.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationReceipts",
                columns: table => new
                {
                    NotificationReceiptID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationReceipts", x => x.NotificationReceiptID);
                    table.ForeignKey(
                        name: "FK_NotificationReceipts_Notifications_NotificationID",
                        column: x => x.NotificationID,
                        principalTable: "Notifications",
                        principalColumn: "NotificationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationReceipts_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationReceipts_NotificationID_UserID",
                table: "NotificationReceipts",
                columns: new[] { "NotificationID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationReceipts_UserID",
                table: "NotificationReceipts",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationReceipts");
        }
    }
}