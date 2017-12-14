using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Handlers;
using Telegram.Bot;
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

      Register<IMessageHandler, MessageTypeAttribute>(builder, assembly);
      Register<IMessageEntityHandler, MessageEntityTypeAttribute>(builder, assembly);

      builder
        .RegisterAssemblyTypes(assembly)
        //.Where(t => typeof(IHandler<>).IsAssignableFrom(t))
        .Where(t => !(new[] { typeof(IMessageHandler), typeof(IMessageEntityHandler) }.Any(_ => _.IsAssignableFrom(t))))
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