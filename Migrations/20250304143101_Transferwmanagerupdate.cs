using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class Transferwmanagerupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ConditionList",
                table: "TransferwmanagerTypes",
                newName: "TransferwmanagerConditionList");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransferwmanagerConditionList",
                table: "TransferwmanagerTypes",
                newName: "ConditionList");
        }
    }
}
