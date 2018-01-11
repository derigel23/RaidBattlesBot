using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NodaTime;
using PokeTrackDecoder.Handlers;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public abstract class UrlLikeMessageEntityHandler : IMessageEntityHandler
  {
    private readonly Message myMessage;
    private readonly Func<MessageEntity, Message, string> myGetUrl;
    private readonly ZonedClock myClock;
    private readonly DateTimeZone myTimeZoneInfo;
    private readonly PokemonInfo myPokemons;
    private readonly GymHelper myGymHelper;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IHttpContextAccessor myHttpContextAccessor;

    protected UrlLikeMessageEntityHandler(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor, Message message, Func<MessageEntity, Message, string> getUrl, ZonedClock clock, DateTimeZone timeZoneInfo, PokemonInfo pokemons, GymHelper gymHelper)
    {
      myMessage = message;
      myGetUrl = getUrl;
      myClock = clock;
      myTimeZoneInfo = timeZoneInfo;
      myPokemons = pokemons;
      myGymHelper = gymHelper;
      myTelemetryClient = telemetryClient;
      myHttpContextAccessor = httpContextAccessor;
    }

    public async Task<bool?> Handle(MessageEntity entity, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var url = myGetUrl(entity, myMessage);
      if (string.IsNullOrEmpty(url) || !url.StartsWith("http"))
        return false;
      using (var httpClient = new HttpClient())
      {
        var poketrackRequest = new HttpRequestMessage(HttpMethod.Head, url);
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
          var poll = pollMessage.Poll = new Poll(myMessage);
          var raid = poll.Raid = new Raid();
          raid.Lon = lon;
          raid.Lat = lat;
          var title = new StringBuilder();

          var messageDate = myMessage.GetMessageDate(myTimeZoneInfo);
          raid.StartTime = messageDate.ToDateTimeOffset();

          var messageText = myMessage.Text;
          var lines = messageText.Split(new[] {'\n'}, 2);
          var firstLine = lines[0].Trim();
          if (InfoGymBotHelper.IsAppropriateUrl(requestUri))
          {
            try
            {
              var poketrackResponseContent = await poketrackResponse.Content.ReadAsStringAsync();
              if (ourRaidInfoBotGymDetector.Match(poketrackResponseContent) is var raidInfoBotGymMatch && raidInfoBotGymMatch.Success)
              {
                raid.PossibleGym = raidInfoBotGymMatch.Value;
              }
            }
            catch (Exception e)
            {
              myTelemetryClient.TrackException(e, myHttpContextAccessor.HttpContext.Properties());
            }
            if (query.TryGetValue("b", out var boss))
            {
              var name = boss.ToString();
              if (name.StartsWith("EGG", StringComparison.OrdinalIgnoreCase)) // EGG
              {
                raid.Name = name.Substring(0, name.Length - 1);
                if (int.TryParse(name.Substring(name.Length - 1, 1), out var raidBossLevel))
                {
                  raid.RaidBossLevel = raidBossLevel;
                }
              }
              else // BOSS
              {
                raid.Name = name;
                //raid.IV = 100; // raid bosses are always 100%
                raid.RaidBossLevel = myPokemons.GetRaidBossLevel(name);
                raid.Pokemon = myPokemons.GetPokemonNumber(name);

                if (lines.Length > 1)
                {
                  InfoGymBotHelper.ProcessMoves(lines[1], raid);
                }
              }

              if (query.TryGetValue("t", out var time) && ParseTime(time, out var dateTime))
              {
                raid.RaidBossEndTime = dateTime;
              }
              else if (query.TryGetValue("tb", out time) && ParseTime(time, out dateTime))
              {
                raid.EndTime = dateTime;
              }
              title
                .AppendFormat("[R{0}] ", raid.RaidBossLevel)
                .Append(raid.Name);
            }
            else
            {
              title.Append(firstLine);
            }
          }
          else
          {
            if (ourPoketrackRaidBossLevelDetector.Match(messageText) is var raidBossLevelMatch && raidBossLevelMatch.Success)
            {
              if (int.TryParse(raidBossLevelMatch.Value, out var raidBossLevel))
              {
                raid.RaidBossLevel = raidBossLevel;
              }
              if (ourPoketrackRaidBossDetector.Match(messageText) is var raidBossMatch && raidBossMatch.Success)
              {
                //raid.IV = 100; // raid bosses are always 100%
                raid.Name = raidBossMatch.Value;
              }
              else
              {
                raid.Name = "Egg";
              }
              title
                .AppendFormat("[R{0}] ", raidBossLevelMatch.Value)
                .Append(raid.Name);

              if ((raidBossLevelMatch.Groups["info"].Success || raidBossMatch.Groups["info"].Success) && (firstLine != "Gym"))
              {
                raid.Gym = firstLine;
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

            if (ParseTimePattern(messageText, ourPoketrackEndTimeDetector, out var dateTime))
            {
              raid.EndTime = dateTime;
            }
            else if (ParseTimePattern(messageText, ourPoketrackStartTimeDetector, out dateTime))
            {
              raid.EndTime = dateTime;
            }
            else if (raid.RaidBossLevel == null)
            {
              if (!ParseTimePattern(messageText, ourPoketrackSpottedDetector, out dateTime))
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
                        int.TryParse(minLeftMatch.Value, out var secLeft))
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

          if (raid.EndTime != null)
          {
            poll.Time = raid.RaidBossEndTime? // adjustemnts
              .Subtract(TimeSpan.FromMinutes(15)) // default offset to the end
              .Round(TimeSpan.FromMinutes(5)); // rounding
            title
              .Append($" ∙ {raid.EndTime:t}");
          }

          bool ParseTimePattern(string text, Regex pattern, out DateTimeOffset parsedDateTime)
          {
            return pattern.Match(text) is var match && match.Success && ParseTime(match.Value, out parsedDateTime);
          }

          bool ParseTime(string value, out DateTimeOffset parsedDateTime)
          {
            if (DateTime.TryParseExact(value, new[] { "HH:mm:ss", "hh:mm tt", "HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault, out var parsedTime) ||
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault, out parsedTime))
            {
              var parsedZonedDateTime = messageDate - Duration.FromTicks(messageDate.TickOfDay) + Duration.FromTicks(parsedTime.TimeOfDay.Ticks);
              parsedDateTime = parsedZonedDateTime.ToDateTimeOffset();
              return true;
            }

            return false;
          }

          string GetMoveAbbreviation(string move) =>
            move.Split(' ', StringSplitOptions.RemoveEmptyEntries).Aggregate("", (agg, s) => agg + s.FirstOrDefault()).ToUpper();
          //if (raid.Move1 != null)
          //{
          //  title.Append(" ∙ ").Append(GetMoveAbbreviation(raid.Move1));
          //  if (raid.Move2 != null)
          //    title.Append('|').Append(GetMoveAbbreviation(raid.Move2));
          //}

          var description = new StringBuilder();
          await myGymHelper.ProcessGym(raid, description, cancellationToken: cancellationToken);

          raid.Title = title.ToString();
          raid.Description = description.ToString();

          return true;
        }

        return null;
      }
    }

    private static readonly Regex ourRaidInfoBotGymDetector = new Regex("(?<=<body>\n).+(?=<br>)");

    private static readonly Regex ourPoketrackStartTimeDetector = new Regex("(?<=Starts at:\\s+).+");
    private static readonly Regex ourPoketrackEndTimeDetector = new Regex("(?<=Ends at:\\s+).+");

    private static readonly Regex ourPoketrackRaidBossDetector = new Regex("(?<=Raid (?<info>Info\\n)?Boss:\\s+).+");
    private static readonly Regex ourPoketrackRaidBossLevelDetector = new Regex("(?<=(Raid (?<info>Info\\n)?)?Level:\\s+).+");

    private static readonly Regex ourPoketrackIVLevelDetector = new Regex("(?<=IV: .+\\()\\d+(?=%\\))");

    private static readonly Regex ourPoketrackMove1Detector = new Regex("(?<=Move 1:\\s+).+");
    private static readonly Regex ourPoketrackMove2Detector = new Regex("(?<=Move 2:\\s+).+");

    private static readonly Regex ourPoketrackSecLeftDetector = new Regex("\\d+(?= sec left)");
    private static readonly Regex ourPoketrackMinLeftDetector = new Regex("\\d+(?= min left)");
    private static readonly Regex ourPoketrackSpottedDetector = new Regex("(?<=spotted at:\\s+).+");
  }
}