using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidBattlesBot.Migrations
{
    public partial class VoteLimits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoteLimits",
                columns: table => new
                {
                    PollId = table.Column<int>(type: "int", nullable: false),
                    Vote = table.Column<int>(type: "int", nullable: false),
                    Limit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteLimits", x => new { x.PollId, x.Vote });
                    table.ForeignKey(
                        name: "FK_VoteLimits_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoteLimits");
        }
    }
}
