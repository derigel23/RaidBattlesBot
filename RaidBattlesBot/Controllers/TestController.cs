using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot;

namespace RaidBattlesBot.Controllers
{
  public class TestController : Controller
  {
    private readonly ITelegramBotClient myBot;

    public TestController(IEnumerable<ITelegramBotClient> bots)
    {
      myBot = bots.FirstOrDefault();
    }
    
    [Route("/decode/{id}")]
    public IActionResult Index(string id)
    {
      if (PollEx.TryGetPollId(id, out var pollId, out var format))
        return Json(new
        {
          pollId,
          format,
          buttons = format?.ToString(),
          encoded = new PollId { Id = pollId, Format = format ?? VoteEnum.None }.ToString()
        });

      return UnprocessableEntity();
    }
  
    [Route("/info/{chatId}/{userId?}")]
    public async Task<IActionResult> Info(long chatId, int? userId, CancellationToken cancellationToken = default)
    {
      dynamic result = new ExpandoObject();
      result["chat"]  = await myBot.GetChatAsync(chatId, cancellationToken);
      if (userId is {} uid)
      {
        result["user"] = await myBot.GetChatMemberAsync(chatId, uid, cancellationToken);
      }
      return Json(result);
    }
  }
}