namespace Team23.TelegramSkeleton
{
  public interface IMessageEntityHandler<in TContext, TResult> : IHandler<MessageEntityEx, TContext, TResult>
  {
  }
}