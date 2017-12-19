using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Module = Autofac.Module;

namespace RaidBattlesBot
{
  public class RegistrationModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.Register(c =>
      {
        var configuration = c.Resolve<IOptions<BotConfiguration>>().Value;
        var botClient = new TelegramBotClient(configuration.BotToken);
        return botClient;
      }).As<ITelegramBotClient>().InstancePerLifetimeScope();
      var assembly = Assembly.GetExecutingAssembly();

      builder.Register(c =>
      {
        return c.Resolve<IMemoryCache>().GetOrCreate("me", entry =>
        {
          entry.SlidingExpiration = TimeSpan.FromMinutes(1);
          return c.Resolve<ITelegramBotClient>().GetMeAsync().GetAwaiter().GetResult();
        });
      });

      builder.RegisterType<RaidService>().InstancePerLifetimeScope();

      Register<IMessageHandler, MessageTypeAttribute>(builder, assembly);
      Register<IMessageEntityHandler, MessageEntityTypeAttribute>(builder, assembly);
      Register<ICallbackQueryHandler, CallbackQueryHandlerAttribute>(builder, assembly);
      Register<IInlineQueryHandler, InlineQueryHandlerAttribute>(builder, assembly);

      builder
        .RegisterAssemblyTypes(assembly)
        //.Where(t => typeof(IHandler<>).IsAssignableFrom(t))
        .Where(t => !(new[] { typeof(IMessageHandler), typeof(IMessageEntityHandler), typeof(ICallbackQueryHandler), typeof(IInlineQueryHandler) }.Any(_ => _.IsAssignableFrom(t))))
        .AsClosedTypesOf(typeof(IHandler<,,>))
        .AsSelf()
        .InstancePerLifetimeScope();
    }

    private static void Register<TInterface, TAttribute>(ContainerBuilder builder, Assembly assembly)
    {
      builder
        .RegisterAssemblyTypes(assembly)
        .Where(t => t.IsAssignableTo<TInterface>())
        .As<TInterface>()
        .AsSelf()
        .WithMetadataFrom<TAttribute>()
        .InstancePerLifetimeScope();
    }
  }
}