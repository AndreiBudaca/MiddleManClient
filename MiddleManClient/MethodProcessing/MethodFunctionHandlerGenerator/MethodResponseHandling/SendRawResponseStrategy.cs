using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling.ResponseHandler;
using MiddleManClient.ServerContracts;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SendRawResponseStrategy : IMethodResultHandlingStrategy
  {
    public async Task HandleResult(object? result, ServerContext context, ResponseWritingHandler responseHandler, CancellationToken cancellationToken = default)
    {
      if (result == null || result is not IAsyncEnumerable<byte[]> resultEnumerable)
      {
        await WriteMetadataIfNeeded(responseHandler, context, cancellationToken);
        return;
      }

      var metadataSent = false;
      await foreach (var item in resultEnumerable.WithCancellation(cancellationToken))
      {
        // Write metadata when first item is generated
        if (!metadataSent)
        {
          metadataSent = true;
          await WriteMetadataIfNeeded(responseHandler, context, cancellationToken);
        }

        await responseHandler.Write(item, cancellationToken);
      }

      if (!metadataSent)
      {
        // If the result enumerable completed without yielding any items, we still need to send the metadata.
        await WriteMetadataIfNeeded(responseHandler, context, cancellationToken);
      }
    }

    public Task<byte[]> HandleResult(object? result, int maxChunkSize)
    {
      throw new NotImplementedException();
    }

    private static async Task WriteMetadataIfNeeded(ResponseWritingHandler responseHandler, ServerContext context, CancellationToken cancellationToken)
    {
      if (context.IsMetadataSet)
      {
        await responseHandler.Write(context.Response.SerializeJson(), cancellationToken);
      }
      else
      {
        await responseHandler.Write(BitConverter.GetBytes(0), cancellationToken);
      }
    }
  }
}
