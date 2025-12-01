using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling;
using MiddleManClient.MethodProcessing.Models;
using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class FunctionHandlerGenerator : IMethodFunctionHandlerGenerator
  {
    public Func<Guid, Task> GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription,  object? methodHandler, int maxMessageLength)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      return async (Guid session) =>
      {
        var clientChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1));
        var serverChannel = await connection.StreamAsChannelAsync<byte[]>("SubscribeToServer", session);
        
        await connection.SendAsync("AddReadChannel", session, clientChannel.Reader);
        
        try
        {
          var result = await MethodInvokingFactory.GetInvokingStrategy(methodDescription)
            .Invoke(methodInfo, methodHandler, serverChannel);

          await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
            .HandleResult(result, clientChannel.Writer, maxMessageLength);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
        }
        finally
        {
          clientChannel.Writer.Complete();
        }
      };
    }
  }
}
