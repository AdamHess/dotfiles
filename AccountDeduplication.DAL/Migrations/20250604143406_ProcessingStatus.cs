using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountDeduplication.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessingStatuses");
        }
    }
}
