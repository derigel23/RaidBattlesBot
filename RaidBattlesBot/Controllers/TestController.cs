using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Controllers
{
  public class TestController : Controller
  {
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
  }
}