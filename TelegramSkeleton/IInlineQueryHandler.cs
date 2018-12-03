using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public interface IInlineQueryHandler : IHandler<InlineQuery, object, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class InlineQueryHandlerAttribute : Attribute, IHandlerAttribute<InlineQuery>
  {
    public string QueryPattern { get; set; }

    public bool ShouldProcess(InlineQuery inlineQuery)
    {
      return string.IsNullOrEmpty(QueryPattern) || Regex.IsMatch(inlineQuery.Query, QueryPattern);
    }
  }
}