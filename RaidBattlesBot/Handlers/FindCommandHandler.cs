using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotCommand("find", "Find user by IGN", BotCommandScopeType.AllPrivateChats, Order = 10)]
  public class FindCommandHandler : IBotCommandHandler
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
      if (!this.ShouldProcess(entity, context)) return null;
      
      var builder = new TextBuilder();
      var nickname = entity.AfterValue.Trim().ToString();
      
      if (!string.IsNullOrEmpty(nickname) && nickname.Length > 3)
      {
        await myBot.SendChatActionAsync(entity.Message.Chat, ChatAction.Typing, cancellationToken);
        
        var found = await myContext.Set<Player>().Where(player => player.Nickname == nickname).Select(_ => _.UserId).ToListAsync(cancellationToken);
        if (found.Count == 0)
        {
          found = (await myContext.Set<Player>().Where(player => player.Nickname.Contains(nickname)).ToListAsync(cancellationToken))
              .Where(player => player.Nickname.Split(new []{','}).Select(nick => nick.Trim()).Contains(nickname, StringComparer.OrdinalIgnoreCase))
              .Select(_ => _.UserId)
              .ToList();
        }

        if (found.Count > 0)
        {
          builder = (await (from v in myContext.Set<Vote>()
              where found.Contains(v.UserId)
              group v by v.UserId into uu
              select uu.OrderByDescending(_ => _.Modified).First())
              .ToListAsync(cancellationToken))
            .Aggregate(builder, (b, vote) =>
              vote.User.GetLink(b, (u, bb) => UserEx.DefaultUserExtractor(u, bb)
                .Append(string.IsNullOrEmpty(u.Username) ? null : $" (@{u.Username})")).NewLine());
        }
      }

      if (builder.Length == 0)
      {
        builder.Append("No one was found.");
      }
      var content = builder.ToTextMessageContent();
      await myBot.SendTextMessageAsync(entity.Message.Chat, content, cancellationToken: cancellationToken);
        
      return false; // processed, but not pollMessage
    }
  }
}