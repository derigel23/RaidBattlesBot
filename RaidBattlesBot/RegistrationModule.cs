using System.Reflection;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Extensions;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Module = Autofac.Module;

namespace RaidBattlesBot
{
  public class RegistrationModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.RegisterInstance(SystemClock.Instance).As<IClock>();
      //builder.RegisterInstance(new FakeClock(SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromHours(5)))).As<IClock>();
      builder.Register(c =>
      {
        var configuration = c.Resolve<IConfiguration>();
        DateTimeZone dateTimeZone = null;
        var timezone = configuration["Timezone"];
        if (!string.IsNullOrEmpty(timezone))
        {
          dateTimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezone);
        }
        return dateTimeZone ?? DateTimeZoneProviders.Tzdb.GetSystemDefault();
      }).InstancePerLifetimeScope();
      builder.Register(c => c.Resolve<IClock>().InZone(c.Resolve<DateTimeZone>()))
        .As<ZonedClock>().InstancePerLifetimeScope();

      builder.RegisterType<GymHelper>().InstancePerLifetimeScope();
      builder.RegisterType<InfoGymBotHelper>().InstancePerLifetimeScope();
      builder.RegisterType<ChatInfo>().InstancePerLifetimeScope();
      builder.RegisterType<RaidService>().InstancePerLifetimeScope();

      builder.RegisterTelegramSkeleton();

      builder.RegisterType<GeoCoder>().SingleInstance();
      builder.RegisterType<GeoCoderEx>().SingleInstance();
      
      builder
        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly())
        .Where(type => type.InheritsOrImplements(typeof(IHostedService)))
        .AsImplementedInterfaces()
        .InstancePerLifetimeScope();

    }
  }
}