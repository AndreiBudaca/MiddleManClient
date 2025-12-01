using MiddleManClient.Extensions;
using System.Text.Json;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SerializeAndSendResultStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ChannelWriter<byte[]> writer, int maxChunkSize)
    {
      if (result is Task taskResult)
      {
        await taskResult.ConfigureAwait(false);
        var resultProperty = taskResult.GetType().GetProperty("Result");
        result = resultProperty?.GetValue(taskResult) ?? default;
      }

      var data = result != null ? JsonSerializer.SerializeToUtf8Bytes(result) : [];

      await writer.WriteChunkedData(maxChunkSize, data);
      writer.Complete();
    }
  }
}
