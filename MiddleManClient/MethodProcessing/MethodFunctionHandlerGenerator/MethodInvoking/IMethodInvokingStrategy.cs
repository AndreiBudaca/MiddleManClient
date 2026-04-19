using MiddleManClient.ServerContracts;
using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public interface IMethodInvokingStrategy
  {
    public Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ChannelReader<byte[]> serverChannel, ServerContext context, byte[] additionalItem, CancellationToken cancellationToken = default);
    public object? Invoke(MethodInfo methodInfo, object? methodHandler, byte[] serverData, ServerContext context);
  }
}
