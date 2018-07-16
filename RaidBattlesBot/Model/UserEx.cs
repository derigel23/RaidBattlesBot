﻿using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.String;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static async Task<StringBuilder> GetLink(this User user, UserInfo userInfo, ParseMode mode = ParseMode.Default, CancellationToken cancellationToken = default)
    {
      var userUri = !await userInfo.IsUserAllowed(user, cancellationToken)
        ? IsNullOrEmpty(user.Username) ? null : $"https://t.me/{user.Username}"
        : $"tg://user?id={user.Id}";

      return new StringBuilder()
        .Link(" ".JoinNonEmpty(user.FirstName, user.LastName).Sanitize(mode), userUri, mode);
    }
  }
}