using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public partial class RaidBattlesContext
  {
    public class Id
    {
      public int NextId { get; set; }
      public int? ExistingId { get; set; }

      public void Deconstruct(out int nextId, out int? existingId)
      {
        nextId = NextId;
        existingId = ExistingId;
      }
    }
    
    public DbQuery<Id> Generator { get; set; }

    public async Task<int> GetNextPollId(CancellationToken cancellationToken = default)
    {
      var queryable = Generator.FromSql(@"
        DECLARE @NEXT int;
        SET @NEXT = NEXT VALUE FOR PollId;
        SELECT NEXT.Id AS NextId, Polls.Id AS ExistingId FROM (SELECT @NEXT AS Id) AS NEXT LEFT JOIN Polls ON Polls.Id = NEXT.Id");

      int nextId = 0;
      int? existingId = null;
      for (var i = 0; i < 5; i++)
      {
        (nextId, existingId) = await queryable.SingleAsync(cancellationToken);
        if (existingId == null)
          break;
      }
      if (existingId != null)
        throw new ArgumentOutOfRangeException($"Duplicate ID from sequence: {existingId}");
      if (nextId == 0)
        throw new ArgumentOutOfRangeException($"Can't retrieve ID from sequence");

      return nextId;
    }
  }
}