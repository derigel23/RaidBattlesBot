using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using LinqToDB.Data;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
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
  public static class Program
  {
    public static void Main(string[] args)
    {
      var builder = CreateWebHostBuilder(args);

      var services = builder.Services;
      
      services.AddApplicationInsightsTelemetry();
      
      // Adds services required for using options.
      services.AddOptions();

      // Register configuration handlers
      services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));
      services.Configure<GeoCoderConfiguration>(builder.Configuration.GetSection("GeoCoder"));
      services.Configure<IngressConfiguration>(builder.Configuration.GetSection("Ingress"));
      services.Configure<Dictionary<string, NotificationChannelInfo>>(builder.Configuration.GetSection("NotificationChannels"));

      var culture = builder.Configuration["Culture"];
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
        if (builder.Environment.IsDevelopment())
        {
          options.EnableSensitiveDataLogging();
        }

        options.UseSqlServer(builder.Configuration.GetConnectionString(ConnectionStringName));
      });
      
      var app = builder.Build();
      
      if (app.Environment.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
          FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "PogoAssets")),
          RequestPath = "/assets",
          OnPrepareResponse = context => context.Context.Response.Headers.Append("Cache-Control", "public,max-age=600")
        });
        DataConnection.TurnTraceSwitchOn();
        DataConnection.WriteTraceLine =
          (message, _, level) => app.Logger.Log(
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

      app.UseRequestLocalization();
      app.UseStaticFiles(); // for regular wwwroot
      app.UseRouting();
      app.MapRazorPages();
      app.MapControllers();
      app.Use((context, next) =>
      {
        if (context.Request.Path.Value is {} path && (path.EndsWith(".php", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase)) ||
            context.Request.GetUri().Segments.Any(segment => segment.StartsWith("wp-", StringComparison.OrdinalIgnoreCase)))
        {
          context.Response.StatusCode = StatusCodes.Status418ImATeapot;
          return Task.CompletedTask;
        }

        return next(context);
      });
      
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

    public const string ConnectionStringName = "RaidBattlesDatabase";
  }
}
