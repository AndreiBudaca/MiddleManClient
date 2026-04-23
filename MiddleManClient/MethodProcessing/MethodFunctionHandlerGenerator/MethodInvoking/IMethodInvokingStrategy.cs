using MiddleManClient.Buffer;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public interface IMethodInvokingStrategy
  {
    public Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ServerContext context, IContentBuffer content, CancellationToken cancellationToken = default);
    public object? Invoke(MethodInfo methodInfo, object? methodHandler, byte[] serverData, ServerContext context);
  }
}
