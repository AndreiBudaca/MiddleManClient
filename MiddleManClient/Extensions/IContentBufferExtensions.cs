using MiddleManClient.Buffer;

namespace MiddleManClient.Extensions
{
  public static class IContentBufferExtensions
  {
    public static async Task BufferAllData(this IContentBuffer contentBuffer, CancellationToken cancellationToken)
    {
      await foreach (var _ in contentBuffer.Read(cancellationToken)) { }
    }
  }
}