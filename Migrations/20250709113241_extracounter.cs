using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class extracounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExtraCounter",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousPlanPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewPlanPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidFee = table.Column<double>(type: "float", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfApproval = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifierId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateVerified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecommenderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateRecommended = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraCounter", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtraCounter");
        }
    }
}
