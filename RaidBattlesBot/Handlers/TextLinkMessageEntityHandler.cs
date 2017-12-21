using System;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using NodaTime;
using RaidBattlesBot.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.TextLink)]
  public class TextLinkMessageEntityHandler : UrlLikeMessageEntityHandler
  {
    public TextLinkMessageEntityHandler(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor, Message message, ZonedClock clock, DateTimeZone timeZoneInfo, ITelegramBotClient bot, PokemonInfo pokemons, GymHelper gymHelper)
      : base(telemetryClient, httpContextAccessor, message, (e, m) => e.Url, clock, timeZoneInfo, bot, pokemons, gymHelper) { }
  }
}