﻿using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
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
    public TextLinkMessageEntityHandler(TelemetryClient telemetryClient, IHttpClientFactory httpClientFactory, Message message, ZonedClock clock, DateTimeZone timeZoneInfo, ITelegramBotClient bot, PokemonInfo pokemons, GymHelper gymHelper, IOptions<BotConfiguration> botConfiguration)
      : base(telemetryClient, httpClientFactory, entity => entity.Url, clock, timeZoneInfo, pokemons, gymHelper, botConfiguration) { }
  }
}