using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RaidBattlesBot.Migrations
{
    public partial class EggRaid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EggRaidId",
                table: "Raids",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Raids_EggRaidId",
                table: "Raids",
                column: "EggRaidId",
                unique: true,
                filter: "[EggRaidId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Raids_Raids_EggRaidId",
                table: "Raids",
                column: "EggRaidId",
                principalTable: "Raids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Raids_Raids_EggRaidId",
                table: "Raids");

            migrationBuilder.DropIndex(
                name: "IX_Raids_EggRaidId",
                table: "Raids");

            migrationBuilder.DropColumn(
                name: "EggRaidId",
                table: "Raids");
        }
    }
}
