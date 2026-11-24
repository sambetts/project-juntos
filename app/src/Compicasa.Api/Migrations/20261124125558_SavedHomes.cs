using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compicasa.Api.Migrations
{
    /// <inheritdoc />
    public partial class SavedHomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedHomes",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HomeId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedHomes", x => new { x.AccountId, x.HomeId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedHomes");
        }
    }
}
