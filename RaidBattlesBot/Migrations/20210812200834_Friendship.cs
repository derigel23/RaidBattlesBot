using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class Friendship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Friendship",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FriendId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PollId = table.Column<int>(type: "int", nullable: true),
                    Modified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendship", x => new { x.Id, x.FriendId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_PollId",
                table: "Friendship",
                column: "PollId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Friendship");
        }
    }
}
