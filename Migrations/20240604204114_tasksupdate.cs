using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class tasksupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Tasks",
                newName: "InspectorId");

            migrationBuilder.AddColumn<string>(
                name: "AssignerId",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignerId",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "InspectorId",
                table: "Tasks",
                newName: "UserId");
        }
    }
}
