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
using Microsoft.Extensions.Logging;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot;

namespace RaidBattlesBot
{
  public class Startup
  {
    private readonly IConfiguration myConfiguration;

    public Startup(IConfiguration configuration)
    {
      myConfiguration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      // Adds services required for using options.
      services.AddOptions();

      // Register configuration handlers
      services.Configure<BotConfiguration>(myConfiguration.GetSection("BotConfiguration"));
      services.Configure<GeoCoderConfiguration>(myConfiguration.GetSection("GeoCoder"));
      services.Configure<IngressConfiguration>(myConfiguration.GetSection("Ingress"));

      services.AddSingleton(provider =>
      {
        var hostingEnvironment = provider.GetRequiredService<IHostingEnvironment>();
        var fileProvider = hostingEnvironment.WebRootFileProvider;
        var namesBuilder = new ConfigurationBuilder().SetFileProvider(fileProvider);
        foreach (var fileInfo in fileProvider.GetDirectoryContents("names"))
        {
          namesBuilder.AddIniFile(fileInfo.PhysicalPath, false, false);
        }
        var raidsBuilder = new ConfigurationBuilder().SetFileProvider(fileProvider);
        raidsBuilder.AddIniFile("pokemon_raids.properties", false, false);

        return new PokemonInfo(namesBuilder.Build(), raidsBuilder.Build(), provider.GetRequiredService<ILoggerFactory>());
      });

      var culture = myConfiguration["Culture"];
      if (!string.IsNullOrEmpty(culture))
      {
        services.Configure<RequestLocalizationOptions>(options =>
        {
          options.DefaultRequestCulture = new RequestCulture(culture);
          options.RequestCultureProviders = null;
        });
      }
      services
        .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
        .AddScoped(x => x
          .GetRequiredService<IUrlHelperFactory>()
          .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext));

      services.AddMemoryCache();
      services.AddHttpClient();
      services.AddHttpClient<IngressClient>();
      services.AddHttpClient<ITelegramBotClient, PoGoTelegramBotClient>();
      services
        .AddMvc(options =>
        {
          options.OutputFormatters.Insert(0, new JsonpMediaTypeFormatter(options.OutputFormatters.OfType<JsonOutputFormatter>().Single()));
        })
        .SetCompatibilityVersion(CompatibilityVersion.Latest)
        .AddControllersAsServices()
        .AddRazorPagesOptions(options =>
        {
          options.Conventions.AddPageRouteWithName("/Portal", "~/{guid:minlength(32)?}", "Portal");
        });

      services.AddDbContextPool<RaidBattlesContext>(options =>
        options.UseSqlServer(myConfiguration.GetConnectionString("RaidBattlesDatabase")));
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
      builder.RegisterModule<RegistrationModule>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
      }

      app
        .UseRequestLocalization()
        .UseStaticFiles() // for regular wwwroot
        .UseMvc();
    }

  }
}
