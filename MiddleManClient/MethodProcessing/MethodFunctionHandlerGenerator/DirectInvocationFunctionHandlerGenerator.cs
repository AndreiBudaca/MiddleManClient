using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling;
using MiddleManClient.MethodProcessing.Models;
using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class DirectInvocationFunctionHandlerGenerator : IMethodFunctionHandlerGenerator
  {
    public void GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      connection.On<byte[], Task<byte[]>>(methodDescription.Name, async (byte[] data) =>
      {
        try
        {
          var (serverContext, bufferOffset) = ServerContextParser.ParseServerContextFromBuffer(data);

          var rawResult = MethodInvokingFactory.GetInvokingStrategy(methodDescription)
            .Invoke(methodInfo, methodHandler, data, bufferOffset, serverContext);

          return await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
            .HandleResult(rawResult, maxMessageLength, serverContext);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
          throw;
        }
      });
    }
  }
}
