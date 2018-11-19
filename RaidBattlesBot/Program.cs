using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace RaidBattlesBot
{
  public class Program
  {
    public static void Main(string[] args) =>
      CreateWebHostBuilder(args).Build().Run();

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
      WebHost
        .CreateDefaultBuilder(args)
        .UseApplicationInsights()
        .UseStartup<Startup>()
        .ConfigureServices(services => services.AddAutofac())
        .ConfigureAppConfiguration((context, builder) =>
        {
          if (context.HostingEnvironment.IsDevelopment())
          {
            builder.AddJsonFile($"appsettings.{EnvironmentName.Development}.user.json", true);
          }
        });
  }
}
