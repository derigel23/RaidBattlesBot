using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RaidBattlesBot.Migrations
{
    public partial class PollIdSequenceWithIncrement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "PollId",
                incrementBy: 3);

            migrationBuilder.RestartSequence(
                name: "PollId",
                startValue: 10100L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "PollId",
                oldIncrementBy: 3);

            migrationBuilder.RestartSequence(
                name: "PollId",
                startValue: 10000L);
        }
    }
}
