using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class NoCacheOnSequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER SEQUENCE PollId NO CYCLE NO CACHE; ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
