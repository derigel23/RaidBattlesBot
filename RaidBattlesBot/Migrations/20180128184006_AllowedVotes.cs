using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RaidBattlesBot.Migrations
{
  public partial class AllowedVotes : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
        name: "AllowedVotes",
        table: "Polls",
        nullable: true);

      migrationBuilder.Sql("UPDATE Votes SET [Team] = CASE [Team] WHEN 1 THEN 2 WHEN 2 THEN 4 WHEN 3 THEN 8 ELSE [Team] END");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
        name: "AllowedVotes",
        table: "Polls");
    }
  }
}