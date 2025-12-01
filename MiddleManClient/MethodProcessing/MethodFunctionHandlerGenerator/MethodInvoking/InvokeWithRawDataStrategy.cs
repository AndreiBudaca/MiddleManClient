using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class InvokeWithRawDataStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ChannelReader<byte[]> serverChannel)
    {
      return methodInfo.Invoke(methodHandler, [serverChannel.ReadAllAsync()]);
    }
  }
}
