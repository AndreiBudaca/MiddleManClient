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

          if (context.IsMetadataSet)
          {
            await writer.WriteChunkedData(maxChunkSize, context.Response.SerializeJson());
          }
          else
          {
            await writer.WriteChunkedData(maxChunkSize, BitConverter.GetBytes(0));
          }
        }

        await writer.WriteChunkedData(maxChunkSize, item);
      }
    }

    public Task<byte[]> HandleResult(object? result, int maxChunkSize, ServerContext context)
    {
      throw new NotImplementedException();
    }
  }
}
