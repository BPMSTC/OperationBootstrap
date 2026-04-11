using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReferralReferrerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferredByEmail",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "ReferredByName",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "ReferredByPhoneNumber",
                table: "Referrals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferredByEmail",
                table: "Referrals",
                type: "varchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferredByName",
                table: "Referrals",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferredByPhoneNumber",
                table: "Referrals",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true);
        }
    }
}
