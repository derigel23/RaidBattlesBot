using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidBattlesBot.Migrations
{
    public partial class NotificationChannels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReplyNotifications_Polls_PollId",
                table: "ReplyNotifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications");

            migrationBuilder.AlterColumn<int>(
                name: "PollId",
                table: "ReplyNotifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MessageId",
                table: "ReplyNotifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications",
                columns: new[] { "ChatId", "FromChatId", "FromMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplyNotifications_MessageId",
                table: "ReplyNotifications",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReplyNotifications_Polls_PollId",
                table: "ReplyNotifications",
                column: "PollId",
                principalTable: "Polls",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReplyNotifications_Polls_PollId",
                table: "ReplyNotifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications");

            migrationBuilder.DropIndex(
                name: "IX_ReplyNotifications_MessageId",
                table: "ReplyNotifications");

            migrationBuilder.AlterColumn<int>(
                name: "PollId",
                table: "ReplyNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MessageId",
                table: "ReplyNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReplyNotifications",
                table: "ReplyNotifications",
                columns: new[] { "ChatId", "MessageId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReplyNotifications_Polls_PollId",
                table: "ReplyNotifications",
                column: "PollId",
                principalTable: "Polls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
