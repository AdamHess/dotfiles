using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountDeduplication.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchRates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_MatchRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchRates_AccountId1",
                table: "MatchRates",
                column: "AccountId1");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRates_AccountId2",
                table: "MatchRates",
                column: "AccountId2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchRates");
        }
    }
}
