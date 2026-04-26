using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling.ResponseHandler;
using MiddleManClient.ServerContracts;
using System.Text.Json;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SerializeAndSendResultStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ServerContext context, ResponseWritingHandler responseHandler, CancellationToken cancellationToken = default)
    {
      var rawResult = await GetRawResult(result, cancellationToken).ConfigureAwait(false);

      await responseHandler.Write(context.IsMetadataSet ? context.Response.SerializeJson() : BitConverter.GetBytes(0), cancellationToken);

      var data = rawResult != null ? JsonSerializer.SerializeToUtf8Bytes(rawResult) : [];
      await responseHandler.Write(data, cancellationToken);
    }

    public async Task<byte[]> HandleResult(object? result, int maxChunkSize)
    {
      var rawResult = await GetRawResult(result, CancellationToken.None).ConfigureAwait(false);

      byte[] dataBytes = [];

      if (rawResult is byte[] byteArrayResult)
      {
        dataBytes = byteArrayResult;
      }
      else if (rawResult != null)
      {
        dataBytes = JsonSerializer.SerializeToUtf8Bytes(rawResult);
      }

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
