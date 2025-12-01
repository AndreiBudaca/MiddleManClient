using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public interface IMethodResultHandlingStrategy
  {
    public Task HandleResult(object? result, ChannelWriter<byte[]> writer, int maxChunkSize);
  }
}
