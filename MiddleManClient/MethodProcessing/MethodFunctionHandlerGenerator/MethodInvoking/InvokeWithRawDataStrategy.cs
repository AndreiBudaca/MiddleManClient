using MiddleManClient.Buffer;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class InvokeWithRawDataStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ServerContext context, IContentBuffer content, CancellationToken cancellationToken = default)
    {
      return methodInfo.Invoke(methodHandler, [context, content.Read(cancellationToken)]);
    }

    public object? Invoke(MethodInfo methodInfo, object? methodHandler, byte[] serverData, ServerContext context)
    {
      throw new NotImplementedException();
    }
  }
}
