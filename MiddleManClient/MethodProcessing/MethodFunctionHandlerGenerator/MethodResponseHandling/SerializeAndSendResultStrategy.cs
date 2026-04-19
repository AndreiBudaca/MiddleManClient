using MiddleManClient.Extensions;
using MiddleManClient.ServerContracts;
using System.Text.Json;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SerializeAndSendResultStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ChannelWriter<byte[]?> writer, int maxChunkSize, ServerContext context, CancellationToken cancellationToken = default)
    {
      var rawResult = await GetRawResult(result, cancellationToken).ConfigureAwait(false);

      // Write metadata after invocation is complete and task is awaited
      await writer.WriteChunkedData(maxChunkSize, context.IsMetadataSet ? context.Response.SerializeJson() : BitConverter.GetBytes(0), cancellationToken);

      var data = rawResult != null ? JsonSerializer.SerializeToUtf8Bytes(rawResult) : [];

      await writer.WriteChunkedData(maxChunkSize, data, cancellationToken);
    }

    public async Task<byte[]> HandleResult(object? result, int maxChunkSize)
    {
      var rawResult = await GetRawResult(result, CancellationToken.None).ConfigureAwait(false);
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

    private static async Task<object?> GetRawResult(object? result, CancellationToken cancellationToken)
    {
      if (result is Task taskResult)
      {
        await taskResult.WaitAsync(cancellationToken).ConfigureAwait(false);
        var resultProperty = taskResult.GetType().GetProperty("Result");
        result = resultProperty?.GetValue(taskResult) ?? default;
      }

      return result;
    }
  }
}
