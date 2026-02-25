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
      var rawResult = await GetRawResult(result).ConfigureAwait(false);

      // Write metadata after invocation is complete and task is awaited
      await writer.WriteChunkedData(maxChunkSize, context.IsMetadataSet ? context.Response.SerializeJson() : BitConverter.GetBytes(0));

      var data = rawResult != null ? JsonSerializer.SerializeToUtf8Bytes(rawResult) : [];

      await writer.WriteChunkedData(maxChunkSize, data);
    }

    public async Task<byte[]> HandleResult(object? result, int maxChunkSize, ServerContext context)
    {
      var metadataBytes = context.IsMetadataSet ? context.Response.SerializeJson() : BitConverter.GetBytes(0);

      var rawResult = await GetRawResult(result).ConfigureAwait(false);
      var dataBytes = rawResult != null ? JsonSerializer.SerializeToUtf8Bytes(rawResult) : [];

      if (metadataBytes.Length + dataBytes.Length <= maxChunkSize)
      {
        var combined = new byte[metadataBytes.Length + dataBytes.Length];
        Buffer.BlockCopy(metadataBytes, 0, combined, 0, metadataBytes.Length);
        Buffer.BlockCopy(dataBytes, 0, combined, metadataBytes.Length, dataBytes.Length);
        
        return combined;
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
