﻿using System.Net.Http;
 using Microsoft.ApplicationInsights;
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
    public UrlMessageEntityHandler(TelemetryClient telemetryClient, IHttpClientFactory httpClientFactory, Message message, ZonedClock clock, DateTimeZone timeZoneInfo, ITelegramBotClient bot, PokemonInfo pokemons, GymHelper gymHelper)
      : base(telemetryClient, httpClientFactory, entity => entity.Value, clock, timeZoneInfo, pokemons, gymHelper) { }
  }
}