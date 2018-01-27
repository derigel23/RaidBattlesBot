using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class ChatInfoEx
  {
    public static async Task<bool> CandEditPoll(this ChatInfo chatInfo, ChatId chat, long? userId, CancellationToken cancellation = default)
    {
      switch (await chatInfo.GetChatMemberStatus(chat, userId, cancellation))
      {
        case ChatMemberStatus.Creator:
        case ChatMemberStatus.Administrator:
          return true;
        default:
          return false;
      }
    }
    
    public static async Task<bool> CanReadPoll(this ChatInfo chatInfo, ChatId chat, long? userId, CancellationToken cancellation = default)
    {
      switch (await chatInfo.GetChatMemberStatus(chat, userId, cancellation))
      {
        case ChatMemberStatus.Creator:
        case ChatMemberStatus.Administrator:
        case ChatMemberStatus.Member:
          return true;
        default:
          return false;
      }
    }
  }
}