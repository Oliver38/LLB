using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class temporaryretails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporaryRetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidFee = table.Column<double>(type: "float", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonForExtention = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoursOfExtension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfApproval = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtendedHoursDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryRetails", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryRetails");
        }
    }
}
