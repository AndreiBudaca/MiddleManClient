using MiddleManClient.Extensions;
using MiddleManClient.ServerContracts;
using System.Text.Json;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SerializeAndSendResultStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ChannelWriter<byte[]?> writer, int maxChunkSize, ServerContext context)
    {
      var rawResult = await GetRawResult(result).ConfigureAwait(false);

      // Write metadata after invocation is complete and task is awaited
      await writer.WriteChunkedData(maxChunkSize, context.IsMetadataSet ? context.Response.SerializeJson() : BitConverter.GetBytes(0));

      var data = rawResult != null ? JsonSerializer.SerializeToUtf8Bytes(rawResult) : [];

      await writer.WriteChunkedData(maxChunkSize, data);
    }

    public async Task<byte[]> HandleResult(object? result, int maxChunkSize)
    {
      var rawResult = await GetRawResult(result).ConfigureAwait(false);
      var dataBytes = rawResult != null ? JsonSerializer.SerializeToUtf8Bytes(rawResult) : [];

      if (dataBytes.Length <= maxChunkSize)
      {
        return dataBytes;
      }
      else
      {
        throw new InvalidOperationException($"Result data exceeds maximum chunk size of {maxChunkSize} bytes.");
      }
    }

    private static async Task<object?> GetRawResult(object? result)
    {
      if (result is Task taskResult)
      {
        await taskResult.ConfigureAwait(false);
        var resultProperty = taskResult.GetType().GetProperty("Result");
        result = resultProperty?.GetValue(taskResult) ?? default;
      }

      return result;
    }
  }
}
