using MiddleManClient.Extensions;
using MiddleManClient.ServerContracts;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SendRawResponseStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ChannelWriter<byte[]> writer, int maxChunkSize, ServerContext context)
    {
      if (result == null || result is not IAsyncEnumerable<byte[]> resultEnumerable)
      {
        writer.Complete();
        return;
      }

      var firstItem = true;

      await foreach (var item in resultEnumerable)
      {
        // Write metadata when first item is generated
        if (firstItem)
        {
          firstItem = false;
          await writer.WriteChunkedData(maxChunkSize, context.Response.SerializeJson());
        }
        await writer.WriteChunkedData(maxChunkSize, item);
      }

      writer.Complete();
    }
  }
}
