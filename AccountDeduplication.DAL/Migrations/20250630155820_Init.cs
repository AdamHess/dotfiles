using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountDeduplication.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 18, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    BillingStreetRaw = table.Column<string>(type: "TEXT", nullable: true),
                    BillingStreetNormalized = table.Column<string>(type: "TEXT", nullable: true),
                    BillingStreet = table.Column<string>(type: "TEXT", nullable: true),
                    BillingHouseNumber = table.Column<string>(type: "TEXT", nullable: true),
                    BillingUnit = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingStreetRaw = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingStreetNormalized = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingHouseNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingStreet = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingUnit = table.Column<string>(type: "TEXT", nullable: true),
                    RecordTypeName = table.Column<string>(type: "TEXT", nullable: true),
                    OtherOrgName = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfRoles = table.Column<int>(type: "INTEGER", nullable: false),
                    NPI = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingState = table.Column<string>(type: "TEXT", nullable: true),
                    BillingState = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingCity = table.Column<string>(type: "TEXT", nullable: true),
                    BillingCity = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingPostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    BillingPostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    GroupingCityState = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingStatuses",
                columns: table => new
                {
                    GroupId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountsInGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ProcessingTime = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingStatuses", x => x.GroupId);
                });

            migrationBuilder.CreateTable(
                name: "GroupPairs",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    MatchPercentage = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPairs", x => new { x.AccountId, x.Phase });
                    table.ForeignKey(
                        name: "FK_GroupPairs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupPairs_Accounts_GroupAccountId",
                        column: x => x.GroupAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MatchRates",
                columns: table => new
                {
                    AccountId1 = table.Column<string>(type: "TEXT", maxLength: 18, nullable: false),
                    AccountId2 = table.Column<string>(type: "TEXT", maxLength: 18, nullable: false),
                    BillingStreetMatch = table.Column<double>(type: "REAL", nullable: false),
                    ShippingAddressMatch = table.Column<double>(type: "REAL", nullable: false),
                    NameMatch = table.Column<double>(type: "REAL", nullable: false),
                    OtherNameMatch = table.Column<double>(type: "REAL", nullable: false),
                    Account1RoleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Account2RoleCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRates", x => new { x.AccountId1, x.AccountId2 });
                    table.ForeignKey(
                        name: "FK_MatchRates_Accounts_AccountId1",
                        column: x => x.AccountId1,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchRates_Accounts_AccountId2",
                        column: x => x.AccountId2,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_BillingCity",
                table: "Accounts",
                column: "BillingCity");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_GroupingCityState",
                table: "Accounts",
                column: "GroupingCityState");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ShippingCity",
                table: "Accounts",
                column: "ShippingCity");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPairs_GroupAccountId",
                table: "GroupPairs",
                column: "GroupAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRates_AccountId2",
                table: "MatchRates",
                column: "AccountId2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupPairs");

            migrationBuilder.DropTable(
                name: "MatchRates");

            migrationBuilder.DropTable(
                name: "ProcessingStatuses");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
