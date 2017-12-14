using Autofac;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using Telegram.Bot;

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
    }
  }
}