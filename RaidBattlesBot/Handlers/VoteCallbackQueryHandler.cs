using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "vote")]
  public class VoteCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly IClock myClock;
    private readonly TimeSpan myVoteTimeout;
    private readonly HashSet<int> myBlackList;

    public VoteCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, IClock clock, IOptions<BotConfiguration> options)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myClock = clock;
      myVoteTimeout = options.Value.VoteTimeout;
      myBlackList = options.Value.BlackList;
    }

    private static readonly Dictionary<VoteEnum?, string> ourResponse = new Dictionary<VoteEnum?, string>
    {
      { VoteEnum.Valor, "Вы проголосовали как Valor" },
      { VoteEnum.Instinct, "Вы проголосовали как Instinct" },
      { VoteEnum.Mystic, "Вы проголосовали как Mystic" },
      { VoteEnum.MayBe, "Вы ещё не решили..." },
      { VoteEnum.Cancel, "Вы передумали!" },
    };

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "vote")
        return (null, false, null);
      
      if (myBlackList.Contains(data.From.Id))
        return (null, false, null);
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return ("Голование подготавливается. Повторите позже", true, null);

      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, cancellationToken))?.Poll;

      if (poll == null)
        return ("Голосование не найдено", true, null);

      var user = data.From;

      var vote = poll.Votes.SingleOrDefault(v => v.UserId == user.Id);
      if (vote == null)
      {
        poll.Votes.Add(vote = new Vote());
      }

      var now = myClock.GetCurrentInstant().ToDateTimeOffset();

      if ((now - vote.Modified) <= myVoteTimeout)
        return ($"Вы слишком быстро голосуете. Попробуйте через {myVoteTimeout.TotalSeconds:0} сек", false, null);
      
      vote.User = user; // update username/firstname/lastname if necessary

      var teamAbbr = callback.ElementAt(2);
      if (!FlagEnums.TryParseFlags(teamAbbr, out VoteEnum team))
        return ("Неправильный голос", true, null);

      var clearTeam = team.RemoveFlags(VoteEnum.Plus);
      if (clearTeam == default)
        clearTeam = VoteEnum.Yes;
      
      vote.Team = team.HasAnyFlags(VoteEnum.Plus) && vote.Team is VoteEnum voted && voted.HasAllFlags(clearTeam) ?
        voted.CommonFlags(VoteEnum.SomePlus).IncreaseVotesCount(1) : clearTeam;

      var changed = myContext.SaveChanges() > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
        return (ourResponse.TryGetValue(vote.Team, out var response) ? response : "Вы проголосовали", false, null);
      }

      return ("Вы уже проголосовали", false, null);
    }
  }
}