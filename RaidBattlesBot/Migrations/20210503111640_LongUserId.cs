using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class LongUserId : Migration
    {
        protected void Before(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey("PK_Votes", "Votes");
            migrationBuilder.DropPrimaryKey("PK_UserSettings", "UserSettings");
            migrationBuilder.DropPrimaryKey("PK_Players", "Players");
        }
        
        protected void After(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddPrimaryKey("PK_Votes", "Votes", new[] { "PollId", "UserId" });
            migrationBuilder.AddPrimaryKey("PK_UserSettings", "UserSettings", new[] { "UserId" });
            migrationBuilder.AddPrimaryKey("PK_Players", "Players", new[] { "UserId" });
        }
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Before(migrationBuilder);
            
            migrationBuilder.AlterColumn<long>(
                name: "BotId",
                table: "Votes",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Votes",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "UserSettings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "FromUserId",
                table: "ReplyNotifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "BotId",
                table: "ReplyNotifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Players",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "BotId",
                table: "Notifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Messages",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "BotId",
                table: "Messages",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
            
            After(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Before(migrationBuilder);
            
            migrationBuilder.AlterColumn<int>(
                name: "BotId",
                table: "Votes",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Votes",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserSettings",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "FromUserId",
                table: "ReplyNotifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BotId",
                table: "ReplyNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Players",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "BotId",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Messages",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BotId",
                table: "Messages",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
            
            After(migrationBuilder);
        }
    }
}
