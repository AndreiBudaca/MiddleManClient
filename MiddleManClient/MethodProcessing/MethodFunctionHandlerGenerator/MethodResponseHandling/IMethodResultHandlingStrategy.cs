using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling.ResponseHandler;
using MiddleManClient.ServerContracts;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public interface IMethodResultHandlingStrategy
  {
    public Task HandleResult(object? result, ServerContext context, ResponseWritingHandler responseHandler, CancellationToken cancellationToken = default);
    public Task<byte[]> HandleResult(object? result, int maxChunkSize);
  }
}
