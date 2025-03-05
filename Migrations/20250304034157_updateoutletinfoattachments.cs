using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class updateoutletinfoattachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Form55",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FingerPrints",
                table: "DirectorDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Form55",
                table: "DirectorDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NatId",
                table: "DirectorDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Form55",
                table: "ManagersParticulars");

            migrationBuilder.DropColumn(
                name: "FingerPrints",
                table: "DirectorDetails");

            migrationBuilder.DropColumn(
                name: "Form55",
                table: "DirectorDetails");

            migrationBuilder.DropColumn(
                name: "NatId",
                table: "DirectorDetails");
        }
    }
}
