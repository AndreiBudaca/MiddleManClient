using Microsoft.AspNetCore.SignalR.Client;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public interface IMethodFunctionHandlerGenerator
  {
    public Func<Guid, Task> GenerateHandler(HubConnection connection, MethodInfo methodInfo, object? methodHandler, int maxMessageLength);

    public static IMethodFunctionHandlerGenerator Default { get => new FunctionHandlerGenerator(); }
  }
}
