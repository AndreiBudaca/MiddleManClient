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

    public void GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength, TimeSpan timeout)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      connection.On(methodDescription.Name, async (Guid session) =>
      {
        var clientChannel = Channel.CreateBounded<byte[]?>(1);
        using var timeoutCts = new CancellationTokenSource(timeout);
        var cancellationToken = timeoutCts.Token;

        try
        {
          var serverChannel = await connection
            .StreamAsChannelAsync<byte[]>("SubscribeToServer", session)
            .WaitAsync(cancellationToken);

          await connection
            .SendAsync("AddReadChannel", session, clientChannel.Reader)
            .WaitAsync(cancellationToken);

          var (serverContext, additionalItem) = await ServerContextParser.ParseServerContextFromStream(serverChannel, cancellationToken);

          var result = await MethodInvokingFactory.GetInvokingStrategy(methodDescription)
            .Invoke(methodInfo, methodHandler, serverChannel, serverContext, additionalItem, cancellationToken);

          await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
            .HandleResult(result, clientChannel.Writer, maxMessageLength, serverContext, cancellationToken);
        }
        catch (Exception)
        {
          // Avoid blocking the error path when the channel is backpressured.
          _ = clientChannel.Writer.TryWrite(null);

          throw;
        }
        finally
        {
          clientChannel.Writer.TryComplete();
        }
      });
    }
  }
}
