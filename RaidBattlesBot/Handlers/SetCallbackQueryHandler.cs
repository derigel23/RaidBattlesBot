using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class SetCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "set";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly ChatInfo myChatInfo;

    public SetCallbackQueryHandler(RaidBattlesContext context, ITelegramBotClient telegramBotClient, ChatInfo chatInfo)
    {
      myContext = context;
      myTelegramBotClient = telegramBotClient;
      myChatInfo = chatInfo;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data?.Split(':');
      if (callback?[0] != ID)
        return (null, false, null);

      if (!await myChatInfo.CandEditPoll(data.Message.Chat, data.From?.Id ,cancellationToken))
        return ("You can't edit the poll.", true, null);

      var chatId = data.Message.Chat.Id;
      var settingsSet = myContext.Set<Settings>();

      var allSettings = await settingsSet.GetSettings(chatId).AsTracking().ToListAsync(cancellationToken);
      Settings settings = null;
      
      switch (callback.ElementAtOrDefault(1))
      {
        case "list":
          return await Return(await SettingsList(chatId, cancellationToken));
        
        case var identifier when int.TryParse(identifier, out var id):
          settings = allSettings.FirstOrDefault(setting => setting.Id == id);
          break;
      }
      
      if (settings == null)
      {
        // create a new
        settings = new Settings { Chat = chatId, Order = allSettings.Select(_ => _.Order).DefaultIfEmpty(-1).Max() + 1, Format = VoteEnum.Standard };
        await settingsSet.AddAsync(settings, cancellationToken);
      }

      string message = null;
      switch (callback.ElementAtOrDefault(2) ?? "")
      {
        case "default":
          settings.Order = allSettings.Select(_ => _.Order).DefaultIfEmpty(1).Min() - 1;
          await myContext.SaveChangesAsync(cancellationToken);
          return await Return(await SettingsList(chatId, cancellationToken), "Default poll format is changed.");

        case "delete":
          settingsSet.Remove(settings);
          await myContext.SaveChangesAsync(cancellationToken);
          return await Return(await SettingsList(chatId, cancellationToken), "Poll format is deleted.");
        
        case var format when FlagEnums.TryParseFlags(format, true, null, out VoteEnum toggleVotes, EnumFormat.DecimalValue):
          settings.Format = FlagEnums.ToggleFlags(settings.Format, toggleVotes);
          // adjust ⁺¹
          if (settings.Format.HasAnyFlags(VoteEnum.Plus) && !settings.Format.HasAnyFlags(VoteEnum.Countable))
          {
            settings.Format = settings.Format.RemoveFlags(VoteEnum.Plus);
          }

          if (await myContext.SaveChangesAsync(cancellationToken) > 0)
          {
            message = "Poll format is changed.";
          }
          goto default;

        default:
          var buttons = new []
            {
              VoteEnum.Host,
              VoteEnum.Valor,
              VoteEnum.Instinct,
              VoteEnum.Mystic,
              VoteEnum.TeamHarmony,
              VoteEnum.Plus1,
              VoteEnum.Remotely,
              VoteEnum.Invitation,
              VoteEnum.MayBe,
              VoteEnum.Yes,
              VoteEnum.Thumbs,
              VoteEnum.HarryPotter,
              VoteEnum.Thanks,
              VoteEnum.PollMode,
              VoteEnum.Cancel,
              VoteEnum.Share
            }
              .Select(format => new []
              {
                InlineKeyboardButton.WithCallbackData($"{(settings.Format.HasAllFlags(format) ? '☑' : '☐')} {format.Format(new StringBuilder())}", $"{ID}:{settings.Id}:{format:D}")
              });

          if (allSettings.FirstOrDefault() is { } existingDefault && existingDefault != settings)
          {
            buttons = buttons.Append(new[]
            {
              InlineKeyboardButton.WithCallbackData("Make as a default", $"{ID}:{settings.Id}:default")
            });
          }
          
          buttons = buttons
            .Append(new [] { InlineKeyboardButton.WithCallbackData("Delete", $"{ID}:{settings.Id}:delete") })
            .Append(new [] { InlineKeyboardButton.WithCallbackData("Back", $"{ID}:list") });

          return await Return((
            settings.Format.Format(new StringBuilder("Selected poll format:").AppendLine()).ToTextMessageContent(),
            new InlineKeyboardMarkup(buttons)), message);
      }

      async Task<(string, bool, string)> Return((InputTextMessageContent content, InlineKeyboardMarkup replyMarkup) pass, string notification = "")
      {
        await myTelegramBotClient.EditMessageTextAsync(data, pass.content, pass.replyMarkup, cancellationToken);

        return (notification, false, null);
      }
    }

    public async Task<(InputTextMessageContent, InlineKeyboardMarkup)> SettingsList(long id, CancellationToken cancellationToken = default)
    {
      var settings = await myContext
        .Set<Settings>()
        .GetSettings(id)
        .ToListAsync(cancellationToken);

      var replyMarkup = new InlineKeyboardMarkup(
        settings
          .Select(setting => new[] { InlineKeyboardButton.WithCallbackData(setting.Format.Format(new StringBuilder()).ToString(), $"{ID}:{setting.Id}") })
          .Concat(new [] { new []{InlineKeyboardButton.WithCallbackData("Create a new", $"{ID}:")}})
          .ToArray());

      return (new StringBuilder("Choose a poll format to edit or create a new:").ToTextMessageContent(), replyMarkup);
    }
  }
}