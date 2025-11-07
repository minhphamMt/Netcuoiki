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
        values: new object[] { 1, new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc), "admin@gmail.com", "Admin User", "$2y$12$YTyoxyxHpp6PDV23yRHRn.4m39bD1zisfhoPdl9dTGaPkEyt8tks.", "Admin" });
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