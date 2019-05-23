using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class SequentialPollId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "PollId",
                oldIncrementBy: 3);

            migrationBuilder.RestartSequence(
                name: "PollId",
                startValue: 100000L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "PollId",
                incrementBy: 3);

            migrationBuilder.RestartSequence(
                name: "PollId",
                startValue: 10100L);
        }
    }
}
