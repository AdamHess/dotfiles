using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountDeduplication.DAL.Migrations
{
    /// <inheritdoc />
    public partial class GroupPair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_GroupPairs_GroupAccountId",
                table: "GroupPairs",
                column: "GroupAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupPairs");
        }
    }
}
