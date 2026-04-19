using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling;
using MiddleManClient.MethodProcessing.Models;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class DirectInvocationFunctionHandlerGenerator : IMethodFunctionHandlerGenerator
  {
    public bool SupportsStreaming => false;

    public void GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength, TimeSpan timeout)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      connection.On<DirectInvocationData, DirectInvocationResponse>(methodDescription.Name, async data =>
      {
        var context = new ServerContext(data.Metadata ?? new HttpRequestMetadata());
        var rawResult = MethodInvokingFactory.GetInvokingStrategy(methodDescription)
          .Invoke(methodInfo, methodHandler, data.Data, context);

        var resultBytes = await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
          .HandleResult(rawResult, maxMessageLength);

        return new DirectInvocationResponse
        {
          Metadata = context.IsMetadataSet ? context.Response : null,
          Data = resultBytes
        };
      });
    }
  }
}
