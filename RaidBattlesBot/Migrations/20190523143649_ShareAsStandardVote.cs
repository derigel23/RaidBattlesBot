using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class ShareAsStandardVote : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"EXEC (N'UPDATE Polls SET [AllowedVotes] = [AllowedVotes] | {Model.VoteEnum.Share:D}')");
            migrationBuilder.Sql($"EXEC (N'UPDATE Settings SET [DefaultAllowedVotes] = [DefaultAllowedVotes] | {Model.VoteEnum.Share:D}')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"EXEC (N'UPDATE Polls SET [AllowedVotes] = [AllowedVotes] & ~{Model.VoteEnum.Share:D}')");
            migrationBuilder.Sql($"EXEC (N'UPDATE Settings SET [DefaultAllowedVotes] = [DefaultAllowedVotes] & ~{Model.VoteEnum.Share:D}')");
        }
    }
}
