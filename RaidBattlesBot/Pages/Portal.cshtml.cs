﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NodaTime;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Pages
{
  public class PortalModel : PageModel
  {
    private readonly IngressClient myIngressClient;

    public Portal Portal { get; private set; }
    
    public PortalModel(RaidBattlesContext db, ZonedClock clock, IngressClient ingressClient)
    {
      myIngressClient = ingressClient;
    }

    public async Task<IActionResult> OnGetAsync(string guid, CancellationToken cancellationToken)
    {
      Portal = guid is { } g ? await myIngressClient.Get(g, cancellationToken: cancellationToken) : null;

      return Page();
    }

    public async Task<IActionResult> OnHeadAsync(CancellationToken cancellationToken)
    {
      return new OkResult();
    }
  }
}