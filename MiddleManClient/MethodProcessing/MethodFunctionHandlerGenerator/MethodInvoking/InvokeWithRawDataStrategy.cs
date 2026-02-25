using MiddleMan.Core.Extensions;
using MiddleManClient.ServerContracts;
using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class InvokeWithRawDataStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ChannelReader<byte[]> serverChannel, ServerContext context, byte[] additionalItem)
    {
      var enumerable = serverChannel.ReadAllAsync();
      if (additionalItem?.Length > 0)
      {
        enumerable = enumerable.PrependItems(additionalItem);
      }

      return methodInfo.Invoke(methodHandler, [context, enumerable]);
    }

    public object? Invoke(MethodInfo methodInfo, object? methodHandler, byte[] serverData, int serverDataOffset, ServerContext context)
    {
      throw new NotImplementedException();
    }
  }
}
