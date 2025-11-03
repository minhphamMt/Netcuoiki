using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTAPLON.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[] { 1, new DateTime(2025, 11, 1, 15, 20, 38, 303, DateTimeKind.Local).AddTicks(6482), "admin@gmail.com", "Admin User", "123", "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1);
        }
    }
}
