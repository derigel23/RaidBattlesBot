using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
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
      
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
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
      
      services
        .AddMvc(options =>
        {
          options.EnableEndpointRouting = false;
          options.OutputFormatters.Insert(0, new JsonpMediaTypeFormatter(options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().Single()));
        })
        .AddNewtonsoftJson()
        .AddApplicationPart(typeof(TelegramController).Assembly)
        .AddRazorPagesOptions(options =>
        {
          options.Conventions.AddPageRouteWithName("/Portal", "~/{guid:minlength(32)?}", "Portal");
        });

      services.AddDbContextPool<RaidBattlesContext>(options =>
      {
        if (Environment?.IsDevelopment() ?? false)
        {
          options.EnableSensitiveDataLogging();
        }

        options.UseSqlServer(myConfiguration.GetConnectionString(ConnectionStringName));
      });
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
      builder.RegisterModule<RegistrationModule>();
    }

    private IWebHostEnvironment Environment { get; set; }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      Environment = env;
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
      }
      else
      {
        app.UseHsts();
        app.UseHttpsRedirection();
      }

      app
        .UseRequestLocalization()
        .UseStaticFiles() // for regular wwwroot
        .UseMvc();
    }

  }
}
