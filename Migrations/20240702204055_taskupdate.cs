using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class taskupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InspectorId",
                table: "Tasks",
                newName: "VerifierId");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecommendationDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommenderId",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecommendationDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecommenderId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "VerificationDate",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "VerifierId",
                table: "Tasks",
                newName: "InspectorId");
        }
    }
}
