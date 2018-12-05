﻿using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using NodaTime;
 using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
 using Team23.TelegramSkeleton;

 namespace RaidBattlesBot.Handlers
{
  public abstract class UrlLikeMessageEntityHandler : IMessageEntityHandler<PollMessage>
  {
    private readonly Func<MessageEntityEx, StringSegment> myGetUrl;
    private readonly ZonedClock myClock;
    private readonly DateTimeZone myTimeZoneInfo;
    private readonly PokemonInfo myPokemons;
    private readonly GymHelper myGymHelper;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IHttpClientFactory myHttpClientFactory;

    protected UrlLikeMessageEntityHandler(TelemetryClient telemetryClient, IHttpClientFactory httpClientFactory, Func<MessageEntityEx, StringSegment> getUrl, ZonedClock clock, DateTimeZone timeZoneInfo, PokemonInfo pokemons, GymHelper gymHelper)
    {
      myGetUrl = getUrl;
      myClock = clock;
      myTimeZoneInfo = timeZoneInfo;
      myPokemons = pokemons;
      myGymHelper = gymHelper;
      myTelemetryClient = telemetryClient;
      myHttpClientFactory = httpClientFactory;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var url = myGetUrl(entity);
      if (StringSegment.IsNullOrEmpty(url) || !url.StartsWith("http", StringComparison.Ordinal))
        return false;
      using (var httpClient = myHttpClientFactory.CreateClient())
      {
        var poketrackRequest = new HttpRequestMessage(HttpMethod.Head, url.ToString());
        if (InfoGymBotHelper.IsAppropriateUrl(poketrackRequest.RequestUri))
        {
          poketrackRequest.Method = HttpMethod.Get;
        }

        var poketrackResponse = await httpClient.SendAsync(poketrackRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var requestUri = poketrackResponse.RequestMessage.RequestUri;
        var query = QueryHelpers.ParseQuery(requestUri.Query);

        bool ParseCoordinate(string str, out decimal coordinate) =>
          decimal.TryParse(str, NumberStyles.Currency, CultureInfo.InvariantCulture, out coordinate);

        bool GetGoogleLocation(Uri uri, string prefix, int position, out decimal lt, out decimal ln)
        {
          lt = 0;
          ln = 0;
          return uri.LocalPath.StartsWith(prefix) && uri.Segments.ElementAtOrDefault(position) is var location &&
            location?.Split(new[] {'@', ',', '/'}, StringSplitOptions.RemoveEmptyEntries) is var locationParts &&
            locationParts?.Length > 1 &&
            ParseCoordinate(locationParts[0], out lt) && ParseCoordinate(locationParts[1], out ln);
        }

        if ((query.TryGetValue("lon", out var lonParameter) && ParseCoordinate(lonParameter, out var lon) &&
             query.TryGetValue("lat", out var latParameter) && ParseCoordinate(latParameter, out var lat)) ||
            (GetGoogleLocation(requestUri, "/maps/search", 3, out lat, out lon) ||
             GetGoogleLocation(requestUri, "/maps/place", 4, out lat, out lon)))
        {
          var title = new StringBuilder();
          var description = new StringBuilder();

          var raid = new Raid { Lon = lon, Lat = lat };

          var messageDate = entity.Message.GetMessageDate(myTimeZoneInfo);
          raid.StartTime = messageDate.ToDateTimeOffset();

          var messageText = entity.Message.Text;
          var lines = messageText.Split(Environment.NewLine.ToCharArray(), 2);
          var firstLine = lines[0].Trim();
          if (InfoGymBotHelper.IsAppropriateUrl(requestUri))
          {
            try
            {
              var poketrackResponseContent = await poketrackResponse.Content.ReadAsStringAsync();
              if (ourRaidInfoBotGymDetector.Match(poketrackResponseContent) is var raidInfoBotGymMatch && raidInfoBotGymMatch.Success && raidInfoBotGymMatch.Value.Length > 0)
              {
                raid.PossibleGym = raidInfoBotGymMatch.Value;
              }
            }
            catch (Exception e)
            {
              myTelemetryClient.TrackExceptionEx(e, pollMessage.GetTrackingProperties());
            }
            if (query.TryGetValue("b", out var boss))
            {
              var movesString = lines.ElementAtOrDefault(1);
              if (movesString?.IndexOf("{подробнее}", StringComparison.Ordinal) is int tail && tail >= 0)
              {
                movesString = movesString.Remove(tail);
              }
              else if (movesString?.IndexOf("📌", StringComparison.Ordinal) is int tail2 && tail2 >= 0)
              {
                movesString = movesString.Remove(tail2);
              }
              raid.ParseRaidInfo(myPokemons, boss.ToString(), movesString);

              if (query.TryGetValue("t", out var time) && messageDate.ParseTime(time, out var dateTime))
              {
                raid.RaidBossEndTime = dateTime;
              }
              else if (query.TryGetValue("tb", out time) && messageDate.ParseTime(time, out dateTime))
              {
                raid.EndTime = dateTime;
              }
            }
            else
            {
              title.Append(firstLine);
            }
          }
          else
          {
            Match raidBossLevelMatch = null;
            Match raidInfoMatch = null;
            if (ourPoketrackRaidBossDetector.Match(messageText) is var raidBossMatch && raidBossMatch.Success)
            {
              raid.Name = raidBossMatch.Value;
              raidBossLevelMatch = ourPoketracLevelDetector.Match(messageText);
              raidInfoMatch = raidBossMatch;
            }
            else if (ourPoketrackRaidLevelDetector.Match(messageText) is var raidLevelMatch && raidLevelMatch.Success)
            {
              raid.Name = "Egg";
              raidBossLevelMatch = raidLevelMatch;
              raidInfoMatch = raidLevelMatch;
            }

            if (raidBossLevelMatch != null)
            {
              if (int.TryParse(raidBossLevelMatch.Value, out var raidBossLevel))
              {
                raid.RaidBossLevel = raidBossLevel;
              }
              if (raidInfoMatch.Groups["info"].Success && (firstLine != "Gym"))
              {
                raid.Gym = firstLine;
              }
            }
            else if (ourKuzminkiBotEggDetector.Match(messageText) is var kuzminkiBotEggMatch && kuzminkiBotEggMatch.Success)
            {
              if (int.TryParse(kuzminkiBotEggMatch.Groups["RaidLevel"].Value, out var raidBossLevel))
              {
                raid.RaidBossLevel = raidBossLevel;
              }
              raid.Name = "Egg";
              raid.Gym = entity.Value.ToString();
              if (messageDate.ParseTimePattern(messageText, ourKuzminkiBotStartTimeDetector, out var kuzminkiBotStartTime))
              {
                raid.EndTime = kuzminkiBotStartTime;
              }
            }
            else if (ourKuzminkiBotRaidDetector.Match(messageText) is var kuzminkiBotRaidMatch && kuzminkiBotRaidMatch.Success)
            {
              if (int.TryParse(kuzminkiBotRaidMatch.Groups["RaidLevel"].Value, out var raidBossLevel))
              {
                raid.RaidBossLevel = raidBossLevel;
              }
              if (int.TryParse(kuzminkiBotRaidMatch.Groups["PokemonNumber"].Value, out var pokemon))
              {
                raid.Pokemon = pokemon;
              }
              raid.Name = kuzminkiBotRaidMatch.Groups["RaidBossName"].Value;
              if (ourKuzminkiBotRaidBossMovesDetector.Match(messageText) is var movesMatch && movesMatch.Success)
              {
                raid.Move1 = movesMatch.Groups["Move1"].Value;
                raid.Move2 = movesMatch.Groups["Move2"].Value;
              }
              raid.Gym = entity.Value.ToString();
              if (messageDate.ParseTimePattern(messageText, ourKuzminkiBotEndTimeDetector, out var kuzminkiBotEndTime))
              {
                raid.EndTime = kuzminkiBotEndTime;
              }
            }
            else
            {
              raid.Name = query.TryGetValue("name", out var name) ? name.ToString() :
                query.TryGetValue("pname", out var pname) ? pname.ToString() : firstLine;

              title.Append(raid.Name);
              if (ourPoketrackIVLevelDetector.Match(messageText) is var ivMatch && ivMatch.Success)
              {
                if (int.TryParse(ivMatch.Value, out var iv))
                {
                  //raid.IV = iv;
                }
                title.Append("\u2009").Append(ivMatch.Value).Append('%');
              }
            }

            if (messageDate.ParseTimePattern(messageText, ourPoketrackEndTimeDetector, out var dateTime))
            {
              raid.EndTime = dateTime;
            }
            else if (messageDate.ParseTimePattern(messageText, ourPoketrackStartTimeDetector, out dateTime))
            {
              raid.EndTime = dateTime;
            }
            else if (raid.RaidBossLevel == null)
            {
              if (!messageDate.ParseTimePattern(messageText, ourPoketrackSpottedDetector, out dateTime))
              {
                dateTime = raid.StartTime ?? (myClock.GetCurrentZonedDateTime().ToDateTimeOffset());
              }

              bool endTimeExplicitlySpecified = false;
              if (ourPoketrackMinLeftDetector.Match(messageText) is var minLeftMatch && minLeftMatch.Success &&
                  int.TryParse(minLeftMatch.Value, out var minLeft))
              {
                dateTime = dateTime.AddMinutes(minLeft);
                endTimeExplicitlySpecified = true;
              }
              else if (ourPoketrackSecLeftDetector.Match(messageText) is var secLeftMatch && secLeftMatch.Success &&
                        int.TryParse(secLeftMatch.Value, out var secLeft))
              {
                dateTime = dateTime.AddSeconds(secLeft);
                endTimeExplicitlySpecified = true;
              }

              if (endTimeExplicitlySpecified)
              {
                raid.EndTime = dateTime;
              }
            }

            if (ourPoketrackMove1Detector.Match(messageText) is var move1Match && move1Match.Success)
            {
              var fastMove = move1Match.Value;
              // fast attack always ends with ' fast'
              if (fastMove.IndexOf(" fast", StringComparison.OrdinalIgnoreCase) is var pos && (pos >= 0))
              {
                fastMove = fastMove.Substring(0, pos);
              }

              raid.Move1 = fastMove;
            }
            if (ourPoketrackMove2Detector.Match(messageText) is var move2Match && move2Match.Success)
            {
              raid.Move2 = move2Match.Value;
            }
          }

          raid.Pokemon = raid.Pokemon ?? myPokemons.GetPokemonNumber(raid.Name);

          await raid.SetTitleAndDescription(title, description, myGymHelper, cancellationToken: cancellationToken);

          pollMessage.Poll = new Poll(entity.Message)
          {
            Raid = raid,
            Time = raid.GetDefaultPollTime()
          };

          return true;
        }

        return null;
      }
    }

    private static readonly Regex ourKuzminkiBotEggDetector = new Regex("Появилось яйцо (?<RaidLevel>\\d+)\\!");
    private static readonly Regex ourKuzminkiBotRaidDetector = new Regex("Доступен рейд (?<RaidLevel>\\d+) на (?<RaidBossName>.+?) \\(#(?<PokemonNumber>\\d+)\\)\\!");
    private static readonly Regex ourKuzminkiBotRaidBossMovesDetector = new Regex("Атаки босса (?<Move1>.+?)/(?<Move2>.+?)\\.");
    private static readonly Regex ourKuzminkiBotStartTimeDetector = new Regex("(?<=Рейд начнётся в\\s+).+?(?=\\.)");
    private static readonly Regex ourKuzminkiBotEndTimeDetector = new Regex("(?<=Рейд закончится в\\s+).+?(?=\\.)");
    
    private static readonly Regex ourRaidInfoBotGymDetector = new Regex("(?<=<body>\n).*?(?=<br>)");

    private static readonly Regex ourPoketrackStartTimeDetector = new Regex("(?<=Starts at:\\s+).+");
    private static readonly Regex ourPoketrackEndTimeDetector = new Regex("(?<=Ends at:\\s+).+");

    private static readonly Regex ourPoketrackRaidBossDetector = new Regex("(?<=Raid (?<info>Info\\n)?Boss:\\s+).+");
    private static readonly Regex ourPoketrackRaidLevelDetector = new Regex("(?<=Raid (?<info>Info\\n)?Level:\\s+).+");
    private static readonly Regex ourPoketracLevelDetector = new Regex("(?<=Level:\\s+).+");

    private static readonly Regex ourPoketrackIVLevelDetector = new Regex("(?<=IV: .+\\()\\d+(?=%\\))");

    private static readonly Regex ourPoketrackMove1Detector = new Regex("(?<=Move 1:\\s+).+");
    private static readonly Regex ourPoketrackMove2Detector = new Regex("(?<=Move 2:\\s+).+");

    private static readonly Regex ourPoketrackSecLeftDetector = new Regex("\\d+(?= sec left)");
    private static readonly Regex ourPoketrackMinLeftDetector = new Regex("\\d+(?= min left)");
    private static readonly Regex ourPoketrackSpottedDetector = new Regex("(?<=spotted at:\\s+).+");
  }
}