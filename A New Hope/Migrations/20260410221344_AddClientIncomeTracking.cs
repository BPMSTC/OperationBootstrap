using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIncomeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarnedIncomeMonthly",
                table: "ClientProfiles");

            migrationBuilder.CreateTable(
                name: "ClientIncomes",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ClientProfileUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IncomeType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientIncomes_ClientProfiles_ClientProfileUserId",
                        column: x => x.ClientProfileUserId,
                        principalTable: "ClientProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientIncomes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClientIncomes_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_ClientProfileUserId",
                table: "ClientIncomes",
                column: "ClientProfileUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_CreatedByUserId",
                table: "ClientIncomes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_DeletedAt",
                table: "ClientIncomes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_IncomeType",
                table: "ClientIncomes",
                column: "IncomeType");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_IsActive",
                table: "ClientIncomes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClientIncomes_UpdatedByUserId",
                table: "ClientIncomes",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientIncomes");

            migrationBuilder.AddColumn<decimal>(
                name: "EarnedIncomeMonthly",
                table: "ClientProfiles",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }
    }
}
