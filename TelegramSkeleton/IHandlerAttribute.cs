namespace Team23.TelegramSkeleton
{
  public interface IHandlerAttribute<in TData, in TContext>
  {
    bool ShouldProcess(TData data, TContext context);
    int Order { get; }
  }
}