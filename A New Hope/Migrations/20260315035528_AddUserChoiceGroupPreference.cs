using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class AddUserChoiceGroupPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserChoiceGroupPreferences",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    InventoryChoiceGroupId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    SelectedInventoryItemId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChoiceGroupPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChoiceGroupPreferences_InventoryChoiceGroups_InventoryCh~",
                        column: x => x.InventoryChoiceGroupId,
                        principalTable: "InventoryChoiceGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChoiceGroupPreferences_InventoryItems_SelectedInventoryI~",
                        column: x => x.SelectedInventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserChoiceGroupPreferences_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserChoiceGroupPreferences_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserChoiceGroupPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserChoiceGroupPreferences_CreatedByUserId",
                table: "UserChoiceGroupPreferences",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChoiceGroupPreferences_DeletedAt",
                table: "UserChoiceGroupPreferences",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserChoiceGroupPreferences_InventoryChoiceGroupId",
                table: "UserChoiceGroupPreferences",
                column: "InventoryChoiceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChoiceGroupPreferences_SelectedInventoryItemId",
                table: "UserChoiceGroupPreferences",
                column: "SelectedInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChoiceGroupPreferences_UpdatedByUserId",
                table: "UserChoiceGroupPreferences",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChoiceGroupPreferences_UserId_InventoryChoiceGroupId",
                table: "UserChoiceGroupPreferences",
                columns: new[] { "UserId", "InventoryChoiceGroupId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChoiceGroupPreferences");
        }
    }
}
