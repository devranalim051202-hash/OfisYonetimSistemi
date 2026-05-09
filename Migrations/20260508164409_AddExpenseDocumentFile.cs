using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfisYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDocumentFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SupplierName",
                table: "Expenses",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Expenses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.Sql(@"
IF COL_LENGTH('Expenses', 'DocumentContentType') IS NULL
BEGIN
    ALTER TABLE [Expenses] ADD [DocumentContentType] nvarchar(100) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Expenses', 'DocumentFilePath') IS NULL
BEGIN
    ALTER TABLE [Expenses] ADD [DocumentFilePath] nvarchar(250) NOT NULL DEFAULT N'';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Expenses', 'DocumentOriginalFileName') IS NULL
BEGIN
    ALTER TABLE [Expenses] ADD [DocumentOriginalFileName] nvarchar(250) NOT NULL DEFAULT N'';
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentContentType",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DocumentFilePath",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DocumentOriginalFileName",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierName",
                table: "Expenses",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Expenses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
