using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType("find - find user by IGN", EntityType = MessageEntityType.BotCommand, Order = -19)]
  public class FindCommandHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public FindCommandHandler(RaidBattlesContext context, ITelegramBotClient bot)
    {
      myContext = context;
      myBot = bot;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (entity.Message.Chat.Type != ChatType.Private)
        return false;
      
      var commandText = entity.AfterValue.Trim();
      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/find":
          var builder = new StringBuilder();
          var nickname = commandText.ToString().ToLowerInvariant();
          if (!string.IsNullOrEmpty(nickname))
          {
            builder = (await myContext
                .Set<Vote>()
                .FromSqlRaw(@"
                  SELECT P.UserId, {1} AS BotId, COALESCE(VV.Username, {0}) AS Username, VV.FirstName, VV.LastName, VV.Team, VV.Modified, -1 AS PollId FROM Players P
                  OUTER APPLY (SELECT TOP 1 * FROM Votes V WHERE V.UserId = P.UserId ORDER BY Modified DESC) VV
                  WHERE UPPER({0}) IN (SELECT UPPER(RTRIM(LTRIM(value))) FROM STRING_SPLIT(P.Nickname,','))", nickname, myBot.BotId)
                .ToListAsync(cancellationToken))
              .Aggregate(builder, (sb, vote) => sb.Append(vote.User.GetLink()).NewLine());

          }

          if (builder.Length == 0)
          {
            builder.Append("No one was found.");
          }
          var content = builder.ToTextMessageContent();
          await myBot.SendTextMessageAsync(entity.Message.Chat, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview, cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage

        default:
          return null;
      }
    }
  }
}