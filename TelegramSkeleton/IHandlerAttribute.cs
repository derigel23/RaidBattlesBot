namespace RaidBattlesBot.Handlers
{
  public interface IHandlerAttribute<in TData>
  {
    bool ShouldProcess(TData data);
  }
}