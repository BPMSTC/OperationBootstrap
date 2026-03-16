using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemOptionToUserItemPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "InventoryItemOptionId",
                table: "UserItemPreferences",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPreferences_InventoryItemOptionId",
                table: "UserItemPreferences",
                column: "InventoryItemOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserItemPreferences_InventoryItemOptions_InventoryItemOption~",
                table: "UserItemPreferences",
                column: "InventoryItemOptionId",
                principalTable: "InventoryItemOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserItemPreferences_InventoryItemOptions_InventoryItemOption~",
                table: "UserItemPreferences");

            migrationBuilder.DropIndex(
                name: "IX_UserItemPreferences_InventoryItemOptionId",
                table: "UserItemPreferences");

            migrationBuilder.DropColumn(
                name: "InventoryItemOptionId",
                table: "UserItemPreferences");
        }
    }
}
