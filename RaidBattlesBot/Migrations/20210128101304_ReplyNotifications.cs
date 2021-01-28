using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class ReplyNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplyNotifications",
                columns: table => new
                {
                    ChatId = table.Column<long>(nullable: false),
                    MessageId = table.Column<int>(nullable: false),
                    BotId = table.Column<int>(nullable: false),
                    PollId = table.Column<int>(nullable: false),
                    FromChatId = table.Column<long>(nullable: false),
                    FromMessageId = table.Column<int>(nullable: false),
                    Modified = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplyNotifications", x => new { x.ChatId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_ReplyNotifications_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReplyNotifications_PollId",
                table: "ReplyNotifications",
                column: "PollId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplyNotifications");
        }
    }
}
