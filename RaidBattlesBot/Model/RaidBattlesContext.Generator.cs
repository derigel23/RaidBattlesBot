using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public partial class RaidBattlesContext
  {
    public async Task<int> GetNextPollId(CancellationToken cancellationToken = default)
    {
      var connection = Database.GetDbConnection();
      using (var dbCommand = connection.CreateCommand())
      {
        dbCommand.CommandType = CommandType.Text;
        dbCommand.CommandText = @"
          DECLARE @NEXT int;
          SET @NEXT = NEXT VALUE FOR PollId;
          SELECT NEXT.Id AS NextId, Polls.Id AS ExistingId FROM (SELECT @NEXT AS Id) AS NEXT LEFT JOIN Polls ON Polls.Id = NEXT.Id";
        for (var i = 0; i < 5; i++)
        {
          var commandBehavior = CommandBehavior.SingleRow;
          if (connection.State != ConnectionState.Open)
          {
            await connection.OpenAsync(cancellationToken);
            commandBehavior |= CommandBehavior.CloseConnection;
          }
          using (var reader = await dbCommand.ExecuteReaderAsync(commandBehavior , cancellationToken))
          {
            while (reader.HasRows && await reader.ReadAsync(cancellationToken))
            {
              var nextId = reader.GetInt32("NextId");
              if (reader.IsDBNull("ExistingId"))
                return nextId;
            }
          }}
      }
      
      throw new ArgumentOutOfRangeException($"Can't retrieve ID from sequence");
    }
  }
}