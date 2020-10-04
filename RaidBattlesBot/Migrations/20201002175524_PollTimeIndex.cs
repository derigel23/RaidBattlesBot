using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class PollTimeIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Polls_Time",
                table: "Polls",
                column: "Time");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Polls_Time",
                table: "Polls");
        }
    }
}
