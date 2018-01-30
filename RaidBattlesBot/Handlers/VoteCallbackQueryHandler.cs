﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "vote")]
  public class VoteCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;

    public VoteCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
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
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return ("Голование подготавливается. Повторите позже", true, null);

      var poll = await myContext
        .Polls
        .Where(_ => _.Id == pollId)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (poll == null)
        return ("Голосование не найдено", true, null);

      var user = data.From;

      var vote = poll.Votes.SingleOrDefault(v => v.UserId == user.Id);
      if (vote == null)
      {
        poll.Votes.Add(vote = new Vote());
      }

      vote.User = user; // update firstname/lastname if necessary

      var teamAbbr = callback.ElementAt(2);
      if (!FlagEnums.TryParseFlags(teamAbbr, out VoteEnum team))
        return ("Неправильный голос", true, null);


      vote.Team = team == VoteEnum.Plus1 ?
        vote.Team?.CommonFlags(VoteEnum.SomePlus) is VoteEnum clearedVote ?
          clearedVote != default ? clearedVote.IncreaseVotesCount(1) : VoteEnum.Yes : VoteEnum.Yes : team;

      var changed = await myContext.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
        return (ourResponse.TryGetValue(vote.Team, out var response) ? response : "Вы проголосовали", false, null);
      }

      return ("Вы уже проголосовали", false, null);
    }
  }
}