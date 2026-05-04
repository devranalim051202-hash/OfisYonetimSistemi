using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfisYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovements_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CompanyId",
                table: "StockMovements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MaterialId",
                table: "StockMovements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProjectId",
                table: "StockMovements",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");
        }
    }
}
