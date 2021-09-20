using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotCommand("tzn", "Time Zone Notifications", BotCommandScopeType.AllChatAdministrators, BotCommandScopeType.AllPrivateChats, Order = 25)]
  public class TimeZoneNotificationCommandHandler : IBotCommandHandler
  {
    private readonly TimeZoneNotifyService myTimeZoneNotifyService;
    private readonly ITelegramBotClient myBot;
    private readonly HashSet<long> mySuperAdministrators;

    public TimeZoneNotificationCommandHandler(IOptions<BotConfiguration> options, TimeZoneNotifyService timeZoneNotifyService, ITelegramBotClient bot)
    {
      myBot = bot;
      myTimeZoneNotifyService = timeZoneNotifyService;
      mySuperAdministrators = options.Value?.SuperAdministrators ?? new HashSet<long>(0);
    }
    
    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(entity, context, ctx => ctx.Message.From?.Id is {} userId && mySuperAdministrators.Contains(userId)))
        return null;

      var (content, replyMarkup) = await myTimeZoneNotifyService.GetSettingsMessage(entity.Message.Chat, cancellationToken: cancellationToken);
      var message = await myBot.SendTextMessageAsync(entity.Message.Chat, content, disableNotification: true, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
      var (_, updatedReplyMarkup) = await myTimeZoneNotifyService.GetSettingsMessage(message.Chat, message.MessageId, cancellationToken);
      await myBot.EditMessageReplyMarkupAsync(message.Chat, message.MessageId, updatedReplyMarkup, cancellationToken);
        
      return false; // processed, but not pollMessage
    }
  }
}