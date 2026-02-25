using MiddleManClient.ServerContracts;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public interface IMethodResultHandlingStrategy
  {
    public Task HandleResult(object? result, ChannelWriter<byte[]> writer, int maxChunkSize, ServerContext context);
    public Task<byte[]> HandleResult(object? result, int maxChunkSize, ServerContext context);
  }
}
