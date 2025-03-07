using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class removalappinfoupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Nationality",
                table: "ApplicationInfo",
                newName: "Pclearance");

            migrationBuilder.AddColumn<string>(
                name: "ApplicantType",
                table: "ApplicationInfo",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCopy",
                table: "ApplicationInfo",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicantType",
                table: "ApplicationInfo");

            migrationBuilder.DropColumn(
                name: "IdCopy",
                table: "ApplicationInfo");

            migrationBuilder.RenameColumn(
                name: "Pclearance",
                table: "ApplicationInfo",
                newName: "Nationality");
        }
    }
}
