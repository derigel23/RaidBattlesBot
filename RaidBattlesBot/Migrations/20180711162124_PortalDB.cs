using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class PortalDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortalId",
                table: "Polls",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Portals",
                columns: table => new
                {
                    Guid = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Image = table.Column<string>(nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,15)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,15)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portals", x => x.Guid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Polls_PortalId",
                table: "Polls",
                column: "PortalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_Portals_PortalId",
                table: "Polls",
                column: "PortalId",
                principalTable: "Portals",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Polls_Portals_PortalId",
                table: "Polls");

            migrationBuilder.DropTable(
                name: "Portals");

            migrationBuilder.DropIndex(
                name: "IX_Polls_PortalId",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "PortalId",
                table: "Polls");
        }
    }
}
