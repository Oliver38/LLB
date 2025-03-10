using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class inspectionmodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Service = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Application = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateApplied = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ventilation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lighting = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SewageDisposalAndDrainage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Toilets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaterSupply = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RubbishDisposal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardOfFood = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoodStorageArrangements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffUniformsAndAccommodation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipmentAndAppointments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HygieneStandards = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inspection");
        }
    }
}
