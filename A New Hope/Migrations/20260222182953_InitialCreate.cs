using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace A_New_Hope.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    AddressLine1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    DefaultPreference = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CategoryGroups",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_CategoryGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryGroups_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CategoryGroups_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClientProfiles",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EmploymentStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    EarnedIncomeMonthly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    IsUnhoused = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HouseholdMembers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ClientUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AgeAsOfDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_Users_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReferringOrganizations",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    AddressLine1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    PrimaryContactName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferringOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferringOrganizations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReferringOrganizations_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CategoryGroupId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ParentId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Categories_CategoryGroups_CategoryGroupId",
                        column: x => x.CategoryGroupId,
                        principalTable: "CategoryGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Categories_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Categories_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ClientUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ReferringOrganizationId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ReferredOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReferredByName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ReferredByPhoneNumber = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true),
                    ReferredByEmail = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_ReferringOrganizations_ReferringOrganizationId",
                        column: x => x.ReferringOrganizationId,
                        principalTable: "ReferringOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IsBaseline = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserItemPreferences",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    InventoryItemId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Preference = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    UpdatedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserItemPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserItemPreferences_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserItemPreferences_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserItemPreferences_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserItemPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryGroupId",
                table: "Categories",
                column: "CategoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryGroupId_Name",
                table: "Categories",
                columns: new[] { "CategoryGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedByUserId",
                table: "Categories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DeletedAt",
                table: "Categories",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UpdatedByUserId",
                table: "Categories",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryGroups_CreatedByUserId",
                table: "CategoryGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryGroups_DeletedAt",
                table: "CategoryGroups",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryGroups_IsActive",
                table: "CategoryGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryGroups_Name",
                table: "CategoryGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryGroups_UpdatedByUserId",
                table: "CategoryGroups",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_CreatedByUserId",
                table: "ClientProfiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_DeletedAt",
                table: "ClientProfiles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_UpdatedByUserId",
                table: "ClientProfiles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_ClientUserId",
                table: "HouseholdMembers",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_CreatedByUserId",
                table: "HouseholdMembers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_DateOfBirth",
                table: "HouseholdMembers",
                column: "DateOfBirth");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_DeletedAt",
                table: "HouseholdMembers",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_UpdatedByUserId",
                table: "HouseholdMembers",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CreatedByUserId",
                table: "InventoryItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_DeletedAt",
                table: "InventoryItems",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_IsActive",
                table: "InventoryItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_IsAvailable",
                table: "InventoryItems",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_UpdatedByUserId",
                table: "InventoryItems",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ClientUserId",
                table: "Referrals",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ClientUserId_ReferredOn",
                table: "Referrals",
                columns: new[] { "ClientUserId", "ReferredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_CreatedByUserId",
                table: "Referrals",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_DeletedAt",
                table: "Referrals",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredOn",
                table: "Referrals",
                column: "ReferredOn");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferringOrganizationId",
                table: "Referrals",
                column: "ReferringOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_UpdatedByUserId",
                table: "Referrals",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferringOrganizations_CreatedByUserId",
                table: "ReferringOrganizations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferringOrganizations_DeletedAt",
                table: "ReferringOrganizations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReferringOrganizations_IsActive",
                table: "ReferringOrganizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ReferringOrganizations_UpdatedByUserId",
                table: "ReferringOrganizations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPreferences_CreatedByUserId",
                table: "UserItemPreferences",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPreferences_DeletedAt",
                table: "UserItemPreferences",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPreferences_InventoryItemId",
                table: "UserItemPreferences",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPreferences_UpdatedByUserId",
                table: "UserItemPreferences",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPreferences_UserId_InventoryItemId",
                table: "UserItemPreferences",
                columns: new[] { "UserId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedByUserId",
                table: "Users",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedAt",
                table: "Users",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedByUserId",
                table: "Users",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientProfiles");

            migrationBuilder.DropTable(
                name: "HouseholdMembers");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "UserItemPreferences");

            migrationBuilder.DropTable(
                name: "ReferringOrganizations");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "CategoryGroups");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
