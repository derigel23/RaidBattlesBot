using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace RaidBattlesBot.Migrations
{
    public partial class PollIdSequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "PollId",
                startValue: 10000L);

            migrationBuilder.AddColumn<int>(
              name: "Id2",
              table: "Polls",
              nullable: true,
              defaultValueSql: "NEXT VALUE FOR PollId");

            migrationBuilder.Sql("EXEC (N'UPDATE Polls SET Id2 = Id')");

            migrationBuilder.DropForeignKey(name: "FK_Messages_Polls_PollId", table: "Messages");

            migrationBuilder.DropForeignKey(name: "FK_Votes_Polls_PollId", table: "Votes");

            migrationBuilder.DropPrimaryKey(name: "PK_Polls", table: "Polls");

          migrationBuilder.DropColumn(
              name: "Id",
              table: "Polls");

          migrationBuilder.RenameColumn(
            name: "Id2",
            table: "Polls",
            newName: "Id");

          migrationBuilder.AlterColumn<int>(
              name: "Id",
              table: "Polls",
              nullable: false,
              defaultValueSql: "NEXT VALUE FOR PollId",
              oldClrType: typeof(int),
              oldNullable: true);

          migrationBuilder.AddPrimaryKey(name: "PK_Polls", table: "Polls", column: "Id");

          migrationBuilder.CreateIndex(
                name: "IX_Polls_Id",
                table: "Polls",
                column: "Id",
                unique: true);

          migrationBuilder.AddForeignKey(
                name: "FK_Votes_Polls_PollId",
                table: "Votes",
                column: "PollId",
                principalTable: "Polls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
              
            migrationBuilder.AddForeignKey(
              name: "FK_Messages_Polls_PollId",
              table: "Messages",
              column: "PollId",
              principalTable: "Polls",
              principalColumn: "Id",
              onDelete: ReferentialAction.Cascade);
         }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new InvalidOperationException();
        }
    }
}
