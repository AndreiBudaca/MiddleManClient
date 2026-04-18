using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling;
using MiddleManClient.MethodProcessing.Models;
using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class StreamingFunctionHandlerGenerator : IMethodFunctionHandlerGenerator
  {
    public bool SupportsStreaming => true;

    public void GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      connection.On(methodDescription.Name, async (Guid session) =>
      {
        var clientChannel = Channel.CreateBounded<byte[]?>(1);
        var serverChannel = await connection.StreamAsChannelAsync<byte[]>("SubscribeToServer", session);

        await connection.SendAsync("AddReadChannel", session, clientChannel.Reader);

        try
        {
          var (serverContext, additionalItem) = await ServerContextParser.ParseServerContextFromStream(serverChannel);

          var result = await MethodInvokingFactory.GetInvokingStrategy(methodDescription)
            .Invoke(methodInfo, methodHandler, serverChannel, serverContext, additionalItem);

          await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
            .HandleResult(result, clientChannel.Writer, maxMessageLength, serverContext);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
          await clientChannel.Writer.WriteAsync(null);
          throw;
        }
        finally
        {
          clientChannel.Writer.Complete();
        }
      });
    }
  }
}
