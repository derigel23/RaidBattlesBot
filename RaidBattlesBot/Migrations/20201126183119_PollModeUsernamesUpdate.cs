using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class PollModeUsernamesUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Settings",
                nullable: false,
                defaultValue: 3999505,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1902353);
            
            migrationBuilder.Sql($"UPDATE Polls SET [AllowedVotes] = [AllowedVotes] | {Model.VoteEnum.PollModeUsernames:D} WHERE [AllowedVotes] & {Model.VoteEnum.PollModeNames:D} = {Model.VoteEnum.PollModeNames:D}");
            migrationBuilder.Sql($"UPDATE Settings SET [Format] = [Format] | {Model.VoteEnum.PollModeUsernames:D} WHERE [Format] & {Model.VoteEnum.PollModeNames:D} = {Model.VoteEnum.PollModeNames:D}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 1902353,
                oldClrType: typeof(int),
                oldDefaultValue: 3999505);
            
            migrationBuilder.Sql($"UPDATE Polls SET [AllowedVotes] = [AllowedVotes] ^ {Model.VoteEnum.PollModeUsernames:D} WHERE [AllowedVotes] & {Model.VoteEnum.PollModeUsernames:D} = {Model.VoteEnum.PollModeUsernames:D}");
            migrationBuilder.Sql($"UPDATE Settings SET [Format] = [Format] ^ {Model.VoteEnum.PollModeUsernames:D} WHERE [Format] & {Model.VoteEnum.PollModeUsernames:D} = {Model.VoteEnum.PollModeUsernames:D}");
        }
    }
}
