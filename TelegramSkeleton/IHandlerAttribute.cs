namespace Team23.TelegramSkeleton
{
  public interface IHandlerAttribute<in TData>
  {
    bool ShouldProcess(TData data);
  }
}