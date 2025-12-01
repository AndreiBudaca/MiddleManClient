using MiddleManClient.Extensions;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SendRawResponseStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ChannelWriter<byte[]> writer, int maxChunkSize)
    {
      if (result == null || result is not IAsyncEnumerable<byte[]> resultEnumerable)
      {
        writer.Complete();
        return;
      }

      await foreach (var item in resultEnumerable)
      {
        await writer.WriteChunkedData(maxChunkSize, item);
      }

      writer.Complete();
    }
  }
}
