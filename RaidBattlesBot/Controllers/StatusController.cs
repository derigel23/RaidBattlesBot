using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RaidBattlesBot.Controllers
{
  public class StatusController : Controller
  {
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
      return Ok("Hi!");
    }
  }
}