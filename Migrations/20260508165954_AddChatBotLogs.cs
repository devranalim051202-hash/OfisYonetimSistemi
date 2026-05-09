using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfisYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddChatBotLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatBotLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CommandText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetectedAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatBotLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatBotLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatBotLogs_UserId",
                table: "ChatBotLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatBotLogs");
        }
    }
}
