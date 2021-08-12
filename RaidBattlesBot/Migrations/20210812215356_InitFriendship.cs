using Microsoft.EntityFrameworkCore.Migrations;

namespace RaidBattlesBot.Migrations
{
    public partial class InitFriendship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.Sql(@$"
INSERT INTO Friendship(Id, FriendId, Type, Modified)
SELECT DISTINCT 
       IIF(v1.UserId <= v2.UserId, v1.UserId, v2.UserId) AS Id,
       IIF(v1.UserId <= v2.UserId, v2.UserId, v1.UserId) AS FriendId,
       1 As Type,
       GETDATE() AS Modified
FROM Votes v1, Votes v2
WHERE v1.PollId = v2.PollId AND v1.UserId != v2.UserId AND
      v1.Team & {Model.VoteEnum.Yes | Model.VoteEnum.Remotely:D} != 0 AND
      v2.Team & {Model.VoteEnum.Invitation:D} != 0
ORDER BY Id, FriendId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.Sql("DELETE * FROM Friendship");
        }
    }
}
