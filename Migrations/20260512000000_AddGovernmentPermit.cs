using System;
using LLB.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260512000000_AddGovernmentPermit")]
    public partial class AddGovernmentPermit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GovernmentPermit",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LG30 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ministry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Council = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Payment = table.Column<double>(type: "float", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Letter_from_the_superior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifierId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateVerified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecommenderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateRecommended = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfApproval = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentPermit", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GovernmentPermit");
        }
    }
}
