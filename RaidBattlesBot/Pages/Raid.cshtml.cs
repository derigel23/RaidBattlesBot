using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Pages
{
    public class RaidModel : PageModel
    {
      private readonly RaidBattlesContext myDbContext;

      public RaidModel(RaidBattlesContext dbContext)
      {
        myDbContext = dbContext;
      }
      
      public Raid Raid { get; private set; }

      public async Task<IActionResult> OnGetAsync(int raidId, CancellationToken cancellationToken = default)
      {
        Raid = await myDbContext.Raids
          .Where(_ => _.Id == raidId)
          .FirstOrDefaultAsync(cancellationToken);
        if (Raid == null)
          return NotFound($"Raid {raidId} not found.");

        return Page();
      }

      public IActionResult OnHead(int raidId)
      {
        return new OkResult();
      }
    }
}