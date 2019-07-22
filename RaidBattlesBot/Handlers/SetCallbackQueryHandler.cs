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
      var callback = data.Data.Split(':');
      if (callback[0] != "set")
        return (null, false, null);

      if (!await myChatInfo.CandEditPoll(data.Message.Chat, data.From?.Id ,cancellationToken))
        return ("У вас недостаточно прав", true, null);

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
        settingsSet.Add(settings);
      }

      string message = null;
      switch (callback.ElementAtOrDefault(2) ?? "")
      {
        case "default":
          settings.Order = allSettings.Select(_ => _.Order).DefaultIfEmpty(1).Min() - 1;
          myContext.SaveChanges();
          return await Return(await SettingsList(chatId, cancellationToken), "Формат голосования по умолчанию изменён");

        case "delete":
          settingsSet.Remove(settings);
          myContext.SaveChanges();
          return await Return(await SettingsList(chatId, cancellationToken), "Формат голосования удалён");
        
        case var format when FlagEnums.TryParseFlags(format, out VoteEnum toggleVotes, EnumFormat.DecimalValue):
          settings.Format = FlagEnums.ToggleFlags(settings.Format, toggleVotes);
          // adjust ⁺¹
          if (settings.Format.HasAnyFlags(VoteEnum.Plus) && !settings.Format.HasAnyFlags(VoteEnum.Countable))
          {
            settings.Format = settings.Format.RemoveFlags(VoteEnum.Plus);
          }

          if (myContext.SaveChanges() > 0)
          {
            message = "Формат голосования изменён";
          }
          goto default;

        default:
          var buttons = new []
            {
              VoteEnum.Team,
              VoteEnum.Plus1,
              VoteEnum.MayBe,
              VoteEnum.Yes,
              VoteEnum.Thumbs,
              VoteEnum.HarryPotter,
              VoteEnum.Cancel,
              VoteEnum.Share
            }
              .Select(format => new []
              {
                InlineKeyboardButton.WithCallbackData($"{(settings.Format.HasAllFlags(format) ? '☑' : '☐')} {format.Format(new StringBuilder())}", $"{ID}:{settings.Id}:{format:D}")
              });

          if (allSettings.FirstOrDefault() is Settings existingDefault && existingDefault != settings)
          {
            buttons = buttons.Append(new[]
            {
              InlineKeyboardButton.WithCallbackData("Сделать по умолчанию", $"{ID}:{settings.Id}:default")
            });
          }
          
          buttons = buttons
            .Append(new [] { InlineKeyboardButton.WithCallbackData("Удалить", $"{ID}:{settings.Id}:delete") })
            .Append(new [] { InlineKeyboardButton.WithCallbackData("Назад", $"{ID}:list") });

          return await Return((
            settings.Format.Format(new StringBuilder("Выбранный формат голосования:").AppendLine()).ToTextMessageContent(),
            new InlineKeyboardMarkup(buttons)), message);
      }

      async Task<(string, bool, string)> Return((InputTextMessageContent content, InlineKeyboardMarkup replyMarkup) pass, string notification = "")
      {
        await myTelegramBotClient.EditMessageTextAsync(data.Message.Chat, data.Message.MessageId,
          pass.content.MessageText, pass.content.ParseMode, pass.content.DisableWebPagePreview, pass.replyMarkup, cancellationToken);

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
          .Concat(new [] { new []{InlineKeyboardButton.WithCallbackData("Создать новый", $"{ID}:")}})
          .ToArray());

      return (new StringBuilder("Выберите формат голосования для редактирования или создайте новый:").ToTextMessageContent(), replyMarkup);
    }
  }
}