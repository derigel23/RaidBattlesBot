using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class PortalTrackable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Modified",
                table: "Portals",
                nullable: true);
            
            migrationBuilder.Sql("UPDATE Portals SET Modified = GetUtcDate() WHERE Modified IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Portals");
        }
    }
}
