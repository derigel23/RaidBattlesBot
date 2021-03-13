using System.Collections.Generic;
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
  
    [Route("/info/{id}")]
    public async Task<IActionResult> Info(long id, CancellationToken cancellationToken = default)
    {
      return Json(await myBot.GetChatAsync(id, cancellationToken));
    }
  }
}