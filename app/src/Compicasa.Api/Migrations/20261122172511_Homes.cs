using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compicasa.Api.Migrations
{
    /// <inheritdoc />
    public partial class Homes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Homes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Agency = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Price = table.Column<int>(type: "int", nullable: true),
                    M2 = table.Column<int>(type: "int", nullable: true),
                    Beds = table.Column<int>(type: "int", nullable: true),
                    Baths = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Municipality = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    District = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Features = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Energy = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GoneAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Homes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Homes_Area",
                table: "Homes",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_Homes_Source_GoneAt",
                table: "Homes",
                columns: new[] { "Source", "GoneAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Homes");
        }
    }
}
