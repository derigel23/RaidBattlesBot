using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaidBattlesBot.Configuration;

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

      services.AddMemoryCache();
      services.AddMvc().AddControllersAsServices();
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
      }

      app
        .UseRequestLocalization()
        .UseStaticFiles()
        .UseMvc();
    }

  }
}
