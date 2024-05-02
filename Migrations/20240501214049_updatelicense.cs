using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class updatelicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_licenseTypes",
                table: "licenseTypes");

            migrationBuilder.RenameTable(
                name: "licenseTypes",
                newName: "LicenseTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LicenseTypes",
                table: "LicenseTypes",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LicenseTypes",
                table: "LicenseTypes");

            migrationBuilder.RenameTable(
                name: "LicenseTypes",
                newName: "licenseTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_licenseTypes",
                table: "licenseTypes",
                column: "Id");
        }
    }
}
