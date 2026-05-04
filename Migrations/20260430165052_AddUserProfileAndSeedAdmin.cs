using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfisYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileAndSeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompanySize",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 5, "Personel hesaplarini yonetir ve kayitlari kontrol eder.", "Mudur" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CompanyName", "CompanySize", "CreatedAt", "Email", "FirstName", "FullName", "LastName", "Password", "PhoneNumber", "RoleId" },
                values: new object[] { 1, "Ofis Yonetim Sistemi", 1, new DateTime(2026, 4, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@ofis.com", "Sistem", "Sistem Admin", "Admin", "Admin123!", "0000000000", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanySize",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");
        }
    }
}
