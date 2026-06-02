using LLB.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLB.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260528090000_AddHotelRoomCountsToOutletInfo")]
    public partial class AddHotelRoomCountsToOutletInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HotelDoubleRooms",
                table: "OutletInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HotelSingleRooms",
                table: "OutletInfo",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HotelDoubleRooms",
                table: "OutletInfo");

            migrationBuilder.DropColumn(
                name: "HotelSingleRooms",
                table: "OutletInfo");
        }
    }
}
