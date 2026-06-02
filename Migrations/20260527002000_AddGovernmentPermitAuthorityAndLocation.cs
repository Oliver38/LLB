using LLB.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527002000_AddGovernmentPermitAuthorityAndLocation")]
    public partial class AddGovernmentPermitAuthorityAndLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "GovernmentPermit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleOfAuthority",
                table: "GovernmentPermit",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "GovernmentPermit");

            migrationBuilder.DropColumn(
                name: "TitleOfAuthority",
                table: "GovernmentPermit");
        }
    }
}
