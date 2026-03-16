using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItemOptions",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InventoryItemId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemOptions_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemOptions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryItemOptions_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemOptions_CreatedByUserId",
                table: "InventoryItemOptions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemOptions_DeletedAt",
                table: "InventoryItemOptions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemOptions_InventoryItemId",
                table: "InventoryItemOptions",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemOptions_InventoryItemId_Name",
                table: "InventoryItemOptions",
                columns: new[] { "InventoryItemId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemOptions_IsActive",
                table: "InventoryItemOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemOptions_UpdatedByUserId",
                table: "InventoryItemOptions",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItemOptions");
        }
    }
}
