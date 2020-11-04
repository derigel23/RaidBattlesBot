using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class StoreBotId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatId_MesssageId",
                table: "Messages");

            migrationBuilder.RenameColumn("InlineMesssageId", "Messages", "InlineMessageId");
            migrationBuilder.RenameColumn("MesssageId", "Messages", "MessageId");

            migrationBuilder.AddColumn<int>(
                name: "BotId",
                table: "Votes",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BotId",
                table: "Messages",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId_MessageId",
                table: "Messages",
                columns: new[] { "ChatId", "MessageId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatId_MessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BotId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "BotId",
                table: "Messages");

            migrationBuilder.RenameColumn("InlineMessageId", "Messages", "InlineMesssageId");
            migrationBuilder.RenameColumn("MessageId", "Messages", "MesssageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId_MesssageId",
                table: "Messages",
                columns: new[] { "ChatId", "MesssageId" });
        }
    }
}
