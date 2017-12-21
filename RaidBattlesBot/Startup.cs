using System.IO;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;

namespace RaidBattlesBot
{
  public class Startup
  {
    private IConfiguration myConfiguration;

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
      Configure<BotConfiguration>(services, myConfiguration, "BotConfiguration");

      services
        .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
        .AddScoped(x => x
          .GetRequiredService<IUrlHelperFactory>()
          .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext));

      services.AddMemoryCache();
      services.AddMvc().AddControllersAsServices();

      services.AddDbContextPool<RaidBattlesContext>(options =>
        options.UseSqlServer(myConfiguration.GetConnectionString("RaidBattlesDatabase")));
    }

    private void Configure<TOptions>(IServiceCollection services, IConfiguration configuration, string sectionName)
      where TOptions : class
    {
      services.Configure<TOptions>(configuration.GetSection(sectionName));
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
        app.UseStaticFiles(new StaticFileOptions
        {
          FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "PogoAssets")),
          RequestPath = "/assets",
          OnPrepareResponse = context => context.Context.Response.Headers.Append("Cache-Control", "public,max-age=600")
        });
      }

      app
        .UseRequestLocalization()
        .UseMvc();
    }

  }
}
