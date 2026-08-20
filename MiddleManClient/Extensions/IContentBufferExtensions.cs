using MiddleManClient.Buffer;

namespace MiddleManClient.Extensions
{
  public static class IContentBufferExtensions
  {
    public static async Task BufferAllData(this IContentBuffer contentBuffer, CancellationToken cancellationToken)
    {
      await foreach (var _ in contentBuffer.Read(cancellationToken)) { }
    }

    public static Task EnsureBuffered(this IContentBuffer contentBuffer, CancellationToken cancellationToken)
    {
      if (contentBuffer.IsCompleted) return Task.CompletedTask;
      return contentBuffer.BufferAllData(cancellationToken);
    }
  }
}