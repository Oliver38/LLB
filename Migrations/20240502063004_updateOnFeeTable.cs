using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class updateOnFeeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FeeId",
                table: "LicenseTypes",
                newName: "TownFee");

            migrationBuilder.AddColumn<double>(
                name: "CityFee",
                table: "LicenseTypes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MunicipaltyFee",
                table: "LicenseTypes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RDCFee",
                table: "LicenseTypes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityFee",
                table: "LicenseTypes");

            migrationBuilder.DropColumn(
                name: "MunicipaltyFee",
                table: "LicenseTypes");

            migrationBuilder.DropColumn(
                name: "RDCFee",
                table: "LicenseTypes");

            migrationBuilder.RenameColumn(
                name: "TownFee",
                table: "LicenseTypes",
                newName: "FeeId");
        }
    }
}
