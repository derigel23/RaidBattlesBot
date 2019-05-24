using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class SettingsFormat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Settings",
                table: "Settings");

            migrationBuilder.RenameColumn(
                name: "DefaultAllowedVotes",
                table: "Settings",
                newName: "Order");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Settings",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<int>(
                name: "Format",
                table: "Settings",
                nullable: false,
                defaultValue: 1822);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Settings",
                table: "Settings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Chat",
                table: "Settings",
                column: "Chat")
                .Annotation("SqlServer:Include", new[] { "Format" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Settings",
                table: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_Settings_Chat",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Settings");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Settings",
                newName: "DefaultAllowedVotes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Settings",
                table: "Settings",
                column: "Chat");
        }
    }
}
