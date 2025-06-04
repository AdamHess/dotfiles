using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountDeduplication.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Migrate : Migration
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
                    BillingStreet = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingStreet = table.Column<string>(type: "TEXT", nullable: true),
                    RecordTypeName = table.Column<string>(type: "TEXT", nullable: true),
                    OtherOrgName = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfRoles = table.Column<int>(type: "INTEGER", nullable: false),
                    NPI = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingState = table.Column<string>(type: "TEXT", nullable: true),
                    BillingState = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingCity = table.Column<string>(type: "TEXT", nullable: true),
                    BillingCity = table.Column<string>(type: "TEXT", nullable: true),
                    Grouping = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
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
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "MatchRates");
        }
    }
}
