using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RaidBattlesBot.Model
{
  public class RaidBattlesContextFactory : IDesignTimeDbContextFactory<RaidBattlesContext>
  {
    public RaidBattlesContext CreateDbContext(string[] args)
    {
      var webHostBuilder = Program.CreateWebHostBuilder(args);
      var configuration = webHostBuilder.Build().Services.GetService<IConfiguration>();
      var connectionSting = configuration.GetConnectionString(Startup.ConnectionStringName);

      var optionsBuilder = new DbContextOptionsBuilder<RaidBattlesContext>();
      optionsBuilder.UseSqlServer(connectionSting, opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds));

      return new RaidBattlesContext(optionsBuilder.Options);
    }
  }
}