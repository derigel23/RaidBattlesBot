using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RaidBattlesBot.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Raids",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(nullable: true),
                    EndTime = table.Column<DateTimeOffset>(nullable: true),
                    Gym = table.Column<string>(nullable: true),
                    Lat = table.Column<decimal>(nullable: true),
                    Lon = table.Column<decimal>(nullable: true),
                    Modified = table.Column<DateTimeOffset>(nullable: true),
                    Move1 = table.Column<string>(nullable: true),
                    Move2 = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    NearByAddress = table.Column<string>(nullable: true),
                    NearByPlaceId = table.Column<string>(nullable: true),
                    Pokemon = table.Column<int>(nullable: true),
                    PossibleGym = table.Column<string>(nullable: true),
                    RaidBossLevel = table.Column<int>(nullable: true),
                    StartTime = table.Column<DateTimeOffset>(nullable: true),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Modified = table.Column<DateTimeOffset>(nullable: true),
                    Owner = table.Column<int>(nullable: false),
                    RaidId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Polls_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ChatId = table.Column<long>(nullable: true),
                    InlineMesssageId = table.Column<string>(nullable: true),
                    MesssageId = table.Column<int>(nullable: true),
                    Modified = table.Column<DateTimeOffset>(nullable: true),
                    PollId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    PollId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    LasttName = table.Column<string>(nullable: true),
                    Modified = table.Column<DateTimeOffset>(nullable: true),
                    Team = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => new { x.PollId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Votes_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_PollId",
                table: "Messages",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_RaidId",
                table: "Polls",
                column: "RaidId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Raids");
        }
    }
}
