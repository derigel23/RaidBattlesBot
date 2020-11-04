using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class UserHomeLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "UserSettings",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lon",
                table: "UserSettings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lat",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "UserSettings");
        }
    }
}
