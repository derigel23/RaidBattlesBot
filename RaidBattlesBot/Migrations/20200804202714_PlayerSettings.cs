using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class PlayerSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Settings",
                nullable: false,
                defaultValue: 329489,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1822);

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    Nickname = table.Column<string>(nullable: true),
                    Modified = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.UserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 1822,
                oldClrType: typeof(int),
                oldDefaultValue: 329489);
        }
    }
}
