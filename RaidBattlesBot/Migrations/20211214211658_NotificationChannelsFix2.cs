using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidBattlesBot.Migrations
{
    public partial class NotificationChannelsFix2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications");

            migrationBuilder.AlterColumn<long>(
                name: "BotId",
                table: "ReplyNotifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications",
                columns: new[] { "ChatId", "FromChatId", "FromMessageId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications");

            migrationBuilder.AlterColumn<long>(
                name: "BotId",
                table: "ReplyNotifications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications",
                columns: new[] { "BotId", "ChatId", "FromChatId", "FromMessageId" });
        }
    }
}
