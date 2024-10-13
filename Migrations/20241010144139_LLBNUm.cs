using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class LLBNUm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LLBNum",
                table: "ApplicationInfo",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LLBNum",
                table: "ApplicationInfo");
        }
    }
}
