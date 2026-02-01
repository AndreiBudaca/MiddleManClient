using MiddleManClient.Extensions;
using MiddleManClient.ServerContracts;
using System.Text.Json;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SerializeAndSendResultStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ChannelWriter<byte[]> writer, int maxChunkSize, ServerContext context)
    {
      if (result is Task taskResult)
      {
        await taskResult.ConfigureAwait(false);
        var resultProperty = taskResult.GetType().GetProperty("Result");
        result = resultProperty?.GetValue(taskResult) ?? default;
      }

      // Write metadata after invocation is complete and task is awaited
      await writer.WriteChunkedData(maxChunkSize, context.Response.SerializeJson());

      var data = result != null ? JsonSerializer.SerializeToUtf8Bytes(result) : [];

      await writer.WriteChunkedData(maxChunkSize, data);
      writer.Complete();
    }
  }
}
