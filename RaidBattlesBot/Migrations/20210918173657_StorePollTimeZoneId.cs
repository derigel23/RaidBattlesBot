using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class StorePollTimeZoneId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Polls",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Polls");
        }
    }
}
