using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryChoiceGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryChoiceGroups",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    MaxSelections = table.Column<int>(type: "int", nullable: false),
                    DisplayLabel = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
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
                    table.PrimaryKey("PK_InventoryChoiceGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryChoiceGroups_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryChoiceGroups_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InventoryChoiceGroupItems",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InventoryChoiceGroupId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    InventoryItemId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
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
                    table.PrimaryKey("PK_InventoryChoiceGroupItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryChoiceGroupItems_InventoryChoiceGroups_InventoryCho~",
                        column: x => x.InventoryChoiceGroupId,
                        principalTable: "InventoryChoiceGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryChoiceGroupItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryChoiceGroupItems_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryChoiceGroupItems_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_CreatedByUserId",
                table: "InventoryChoiceGroupItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_DeletedAt",
                table: "InventoryChoiceGroupItems",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_InventoryChoiceGroupId",
                table: "InventoryChoiceGroupItems",
                column: "InventoryChoiceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_InventoryChoiceGroupId_InventoryIt~",
                table: "InventoryChoiceGroupItems",
                columns: new[] { "InventoryChoiceGroupId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_InventoryItemId",
                table: "InventoryChoiceGroupItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_IsActive",
                table: "InventoryChoiceGroupItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroupItems_UpdatedByUserId",
                table: "InventoryChoiceGroupItems",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroups_CreatedByUserId",
                table: "InventoryChoiceGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroups_DeletedAt",
                table: "InventoryChoiceGroups",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroups_IsActive",
                table: "InventoryChoiceGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroups_Name",
                table: "InventoryChoiceGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChoiceGroups_UpdatedByUserId",
                table: "InventoryChoiceGroups",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryChoiceGroupItems");

            migrationBuilder.DropTable(
                name: "InventoryChoiceGroups");
        }
    }
}
