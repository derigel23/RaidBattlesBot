namespace Team23.TelegramSkeleton
{
  public interface IMessageEntityHandler<in TContext> : IHandler<MessageEntityEx, TContext, bool?>
  {
  }
}