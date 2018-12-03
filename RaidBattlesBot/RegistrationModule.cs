using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Extensions;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
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
//      var assembly = Assembly.GetExecutingAssembly();

//      Register<IMessageHandler, MessageTypeAttribute>(builder, assembly);
//      Register<IMessageEntityHandler, MessageEntityTypeAttribute>(builder, assembly);
//      Register<ICallbackQueryHandler, CallbackQueryHandlerAttribute>(builder, assembly);
//      Register<IInlineQueryHandler, InlineQueryHandlerAttribute>(builder, assembly);

//      builder
//        .RegisterAssemblyTypes(assembly)
//        //.Where(t => typeof(IHandler<>).IsAssignableFrom(t))
//        .Where(t => !(new[] { typeof(IMessageHandler), typeof(IMessageEntityHandler), typeof(ICallbackQueryHandler), typeof(IInlineQueryHandler) }.Any(_ => _.IsAssignableFrom(t))))
//        .AsClosedTypesOf(typeof(IHandler<,,>))
//        .AsSelf()
//        .InstancePerLifetimeScope();
    }

//    private static void Register<TInterface, TAttribute>(ContainerBuilder builder, Assembly assembly)
//    {
//      builder
//        .RegisterAssemblyTypes(assembly)
//        .AssignableTo<TInterface>()
//        .AsImplementedInterfaces()
//        .AsSelf()
//        .WithMetadataFrom<TAttribute>()
//        .InstancePerLifetimeScope();
//    }
  }
}