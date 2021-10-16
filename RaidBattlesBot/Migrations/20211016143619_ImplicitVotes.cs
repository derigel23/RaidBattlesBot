using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidBattlesBot.Migrations
{
    public partial class ImplicitVotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 71108369,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 3999505);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Format",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 3999505,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 71108369);
        }
    }
}
