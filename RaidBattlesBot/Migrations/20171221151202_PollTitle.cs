using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RaidBattlesBot.Migrations
{
    public partial class PollTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Polls_Raids_RaidId",
                table: "Polls");

            migrationBuilder.AlterColumn<int>(
                name: "RaidId",
                table: "Polls",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Polls",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_Raids_RaidId",
                table: "Polls",
                column: "RaidId",
                principalTable: "Raids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Polls_Raids_RaidId",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Polls");

            migrationBuilder.AlterColumn<int>(
                name: "RaidId",
                table: "Polls",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_Raids_RaidId",
                table: "Polls",
                column: "RaidId",
                principalTable: "Raids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
