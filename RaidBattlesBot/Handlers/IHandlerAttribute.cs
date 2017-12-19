namespace RaidBattlesBot.Handlers
{
  public interface IHandlerAttribute<in TData, in TContext>
  {
    bool ShouldProcess(TData data, TContext context);
  }
}