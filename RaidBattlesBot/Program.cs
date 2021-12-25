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
        .UseStartup<Startup>()
        .ConfigureServices(services => services.AddAutofac())
        .ConfigureAppConfiguration((context, builder) =>
        {
          builder.AddJsonFile(
            string.IsNullOrEmpty(context.HostingEnvironment.EnvironmentName)
              ? $"appsettings.user.json"
              : $"appsettings.{context.HostingEnvironment.EnvironmentName}.user.json", true);
        });
  }
}
