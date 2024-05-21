using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    /// <inheritdoc />
    public partial class tablesOutAndAttach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ManagersParticularss",
                table: "ManagersParticularss");

            migrationBuilder.RenameTable(
                name: "ManagersParticularss",
                newName: "ManagersParticulars");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ManagersParticulars",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ApplicationID",
                table: "ManagersParticulars",
                newName: "ApplicationId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Surname",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationId",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateUpdated",
                table: "ManagersParticulars",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ManagersParticulars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ManagersParticulars",
                table: "ManagersParticulars",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AttachmentInfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentInfo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentInfo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ManagersParticulars",
                table: "ManagersParticulars");

            migrationBuilder.DropColumn(
                name: "DateUpdated",
                table: "ManagersParticulars");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "ManagersParticulars");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ManagersParticulars");

            migrationBuilder.RenameTable(
                name: "ManagersParticulars",
                newName: "ManagersParticularss");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ManagersParticularss",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "ManagersParticularss",
                newName: "ApplicationID");

            migrationBuilder.AlterColumn<string>(
                name: "Surname",
                table: "ManagersParticularss",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "ManagersParticularss",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ManagersParticularss",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationID",
                table: "ManagersParticularss",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "ManagersParticularss",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ManagersParticularss",
                table: "ManagersParticularss",
                column: "Id");
        }
    }
}
