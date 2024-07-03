using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class queryupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Query",
                table: "Queries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Query",
                table: "Queries");
        }
    }
}
