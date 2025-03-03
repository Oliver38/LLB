using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class removals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegionName",
                table: "TransferRegion",
                newName: "Removalame");

            migrationBuilder.CreateTable(
                name: "RemovalRegion",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RemovalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemovalRegion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemovalTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RemovalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityFee = table.Column<double>(type: "float", nullable: false),
                    MunicipaltyFee = table.Column<double>(type: "float", nullable: false),
                    TownFee = table.Column<double>(type: "float", nullable: false),
                    RDCFee = table.Column<double>(type: "float", nullable: false),
                    ConditionList = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RemovalInstructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemovalTypes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemovalRegion");

            migrationBuilder.DropTable(
                name: "RemovalTypes");

            migrationBuilder.RenameColumn(
                name: "Removalame",
                table: "TransferRegion",
                newName: "RegionName");
        }
    }
}
