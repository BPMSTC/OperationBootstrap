using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class SyncClientIncomeSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientIncomes_ClientIncomes_ClientIncomeId",
                table: "ClientIncomes");

            migrationBuilder.DropIndex(
                name: "IX_ClientIncomes_ClientIncomeId",
                table: "ClientIncomes");

            migrationBuilder.DropColumn(
                name: "ClientIncomeId",
                table: "ClientIncomes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "ClientIncomeId",
                table: "ClientIncomes",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_ClientIncomeId",
                table: "ClientIncomes",
                column: "ClientIncomeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientIncomes_ClientIncomes_ClientIncomeId",
                table: "ClientIncomes",
                column: "ClientIncomeId",
                principalTable: "ClientIncomes",
                principalColumn: "Id");
        }
    }
}
