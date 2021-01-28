using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class TrackBotIdForNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BotId",
                table: "Notifications",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotId",
                table: "Notifications");
        }
    }
}
