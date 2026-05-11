using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfisYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddEmlakRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 6, "Emlak ve daire satis sureclerini takip eder.", "Emlak" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
