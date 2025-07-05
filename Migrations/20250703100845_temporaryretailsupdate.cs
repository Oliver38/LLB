using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class temporaryretailsupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExtendedHoursDate",
                table: "TemporaryRetails",
                newName: "TemporaryRetailsDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemporaryRetailsDate",
                table: "TemporaryRetails",
                newName: "ExtendedHoursDate");
        }
    }
}
