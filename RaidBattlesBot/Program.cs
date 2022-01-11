using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace RaidBattlesBot
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = CreateWebHostBuilder(args);
      var startup = new Startup(builder.Configuration);
      startup.ConfigureServices(builder.Services, builder.Environment);
      var app = builder.Build();
      startup.Configure(app, app.Environment, app.Logger);
      app.Run();
    }

    public static WebApplicationBuilder CreateWebHostBuilder(string[] args)
    {
      var webApplicationBuilder = WebApplication.CreateBuilder(args);
      webApplicationBuilder.Host.ConfigureAppConfiguration((context, builder) =>
        {
          builder.AddJsonFile(
            string.IsNullOrEmpty(context.HostingEnvironment.EnvironmentName)
              ? $"appsettings.user.json"
              : $"appsettings.{context.HostingEnvironment.EnvironmentName}.user.json", true);
        });
      webApplicationBuilder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(builder => builder.RegisterModule<RegistrationModule>()));
      return webApplicationBuilder;
    }
  }
}
