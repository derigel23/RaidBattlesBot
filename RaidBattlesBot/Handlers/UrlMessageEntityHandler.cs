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
  [MessageEntityType(EntityType = MessageEntityType.Url)]
  public class UrlMessageEntityHandler : UrlLikeMessageEntityHandler
  {
    public UrlMessageEntityHandler(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor, Message message, ZonedClock clock, DateTimeZone timeZoneInfo, ITelegramBotClient bot, PokemonInfo pokemons, GymHelper gymHelper)
      : base(telemetryClient, httpContextAccessor, message, (e, m) => m.Text.Substring(e.Offset, e.Length), clock, timeZoneInfo, bot, pokemons, gymHelper) { }
  }
}