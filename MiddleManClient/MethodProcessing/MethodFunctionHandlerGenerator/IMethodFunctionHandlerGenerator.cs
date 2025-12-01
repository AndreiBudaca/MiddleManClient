using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.MethodProcessing.Models;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public interface IMethodFunctionHandlerGenerator
  {
    public Func<Guid, Task> GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength);

    public static IMethodFunctionHandlerGenerator Default { get => new FunctionHandlerGenerator(); }
  }
}
