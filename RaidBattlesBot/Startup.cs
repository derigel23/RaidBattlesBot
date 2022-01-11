using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot
{
  public class Startup
  {
    private readonly IConfiguration myConfiguration;

    public Startup(IConfiguration configuration)
    {
      myConfiguration = configuration;
    }

    public const string ConnectionStringName = "RaidBattlesDatabase";
      
    public void ConfigureServices(IServiceCollection services, IWebHostEnvironment env)
    {
      services.AddApplicationInsightsTelemetry();
      
      // Adds services required for using options.
      services.AddOptions();

      // Register configuration handlers
      services.Configure<BotConfiguration>(myConfiguration.GetSection("BotConfiguration"));
      services.Configure<GeoCoderConfiguration>(myConfiguration.GetSection("GeoCoder"));
      services.Configure<IngressConfiguration>(myConfiguration.GetSection("Ingress"));
      services.Configure<Dictionary<string, NotificationChannelInfo>>(myConfiguration.GetSection("NotificationChannels"));

     var culture = myConfiguration["Culture"];
      if (!string.IsNullOrEmpty(culture))
      {
        services.Configure<RequestLocalizationOptions>(options =>
        {
          options.DefaultRequestCulture = new RequestCulture(culture);
          options.RequestCultureProviders = ArraySegment<IRequestCultureProvider>.Empty;
        });
      }

      services
        .AddHttpContextAccessor()
        .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
        .AddScoped(x => x
          .GetRequiredService<IUrlHelperFactory>()
          .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext ?? new ActionContext()));

      services.AddMemoryCache();
      services.AddHttpClient();
      services.AddHttpClient<IngressClient>();
      services.AddHttpClient<YandexMapsClient>();
      services.AddHttpClient<PoGoToolsClient>();
      services.RegisterTelegramClients<PoGoTelegramBotClient>(provider => provider.GetService<IOptions<BotConfiguration>>()?.Value?.BotTokens);

      services.AddRazorPages();
      services
        .AddControllers(options =>
        {
          options.OutputFormatters.Insert(0, new JsonpMediaTypeFormatter(options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().Single()));
        })
        .AddNewtonsoftJson()
        .AddApplicationPart(typeof(TelegramController).Assembly);
      
      services.AddDbContextPool<RaidBattlesContext>(options =>
      {
        if (env.IsDevelopment())
        {
          options.EnableSensitiveDataLogging();
        }

        options.UseSqlServer(myConfiguration.GetConnectionString(ConnectionStringName));
      });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger logger)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
          FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "PogoAssets")),
          RequestPath = "/assets",
          OnPrepareResponse = context => context.Context.Response.Headers.Append("Cache-Control", "public,max-age=600")
        });
        DataConnection.TurnTraceSwitchOn();
        DataConnection.WriteTraceLine =
          (message, _, level) => logger.Log(
            level switch
            {
              TraceLevel.Error => LogLevel.Error,
              TraceLevel.Warning => LogLevel.Warning,
              TraceLevel.Info => LogLevel.Information,
              TraceLevel.Verbose => LogLevel.Trace,
              TraceLevel.Off => LogLevel.None,
              _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            }, message);
      }
      else
      {
        app.UseHsts();
        app.UseHttpsRedirection();
      }

      app
        .UseRequestLocalization()
        .UseStaticFiles() // for regular wwwroot
        .UseRouting()
        .UseEndpoints(builder =>
        {
          builder.MapRazorPages();
          builder.MapControllers();
          builder.Map("wp_content/{**rest}", context => Task.FromResult(context.Response.StatusCode = StatusCodes.Status418ImATeapot));
        });
    }

  }
}
