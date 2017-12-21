using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public interface IInlineQueryHandler : IHandler<InlineQuery, object, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class InlineQueryHandlerAttribute : Attribute, IHandlerAttribute<InlineQuery, object>
  {
    public string QueryPrefix { get; set; }

    public bool ShouldProcess(InlineQuery inlineQuery, object context)
    {
      return string.IsNullOrEmpty(QueryPrefix) || inlineQuery.Query.StartsWith(QueryPrefix);
    }
  }
}