﻿using System.Reflection;
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
      builder.RegisterInstance(DateTimeZoneProviders.Tzdb).As<IDateTimeZoneProvider>();
      builder.RegisterType<TimeZoneService>().SingleInstance();
      builder.RegisterType<WebHookSaltProvider>().SingleInstance().AsImplementedInterfaces();
      builder.Register(c =>
      {
        var configuration = c.Resolve<IConfiguration>();
        DateTimeZone dateTimeZone = null;
        var timezone = configuration["Timezone"];
        if (!string.IsNullOrEmpty(timezone))
        {
          dateTimeZone = c.Resolve<IDateTimeZoneProvider>().GetZoneOrNull(timezone);
        }
        return dateTimeZone ?? c.Resolve<IDateTimeZoneProvider>().GetSystemDefault();
      }).InstancePerLifetimeScope();
      builder.Register(c => c.Resolve<IClock>().InZone(c.Resolve<DateTimeZone>()))
        .As<ZonedClock>().InstancePerLifetimeScope();

      builder.RegisterType<GymHelper>().InstancePerLifetimeScope();
      builder.RegisterType<ChatInfo>().InstancePerLifetimeScope();
      builder.RegisterType<RaidService>().InstancePerLifetimeScope();
      builder.RegisterType<FriendshipService>().InstancePerLifetimeScope();
      builder.RegisterType<TimeZoneNotifyService>().InstancePerLifetimeScope();

      builder.RegisterTelegramSkeleton<PoGoTelegramBotClient>();

      builder.RegisterType<GeoCoder>().SingleInstance();
      builder.RegisterType<GeoCoderEx>().SingleInstance();

      builder
        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly())
        .Where(type => type.InheritsOrImplements(typeof(IHostedService)))
        .AsSelf()
        .AsImplementedInterfaces()
        .SingleInstance();

      builder
        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly())
        .Where(type => type.InheritsOrImplements(typeof(IBackgroundServiceWorker)))
        .AsSelf()
        .AsImplementedInterfaces()
        .InstancePerLifetimeScope();
    }
  }
}