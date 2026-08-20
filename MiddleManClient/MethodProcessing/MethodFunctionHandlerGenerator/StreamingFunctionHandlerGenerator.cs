using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.Buffer;
using MiddleManClient.Extensions;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling;
using MiddleManClient.MethodProcessing.Models;
using MiddleManClient.ServerContracts;
using System.Reflection;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class StreamingFunctionHandlerGenerator(Func<IAsyncEnumerable<byte[]>, IContentBuffer> contentBufferFactory) : IMethodFunctionHandlerGenerator
  {
    private readonly Func<IAsyncEnumerable<byte[]>, IContentBuffer> _contentBufferFactory = contentBufferFactory;

    public bool SupportsStreaming => true;

    public StreamingFunctionHandlerGenerator() : this(content => new MemoryBuffer(content, int.MaxValue)) { }

    public void GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength, TimeSpan timeout)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      connection.On(methodDescription.Name, async (Guid session, HttpRequestMetadata metadata) =>
      {
        var clientChannel = Channel.CreateBounded<byte[]?>(1);
        using var timeoutCts = new CancellationTokenSource(timeout);
        var cancellationToken = timeoutCts.Token;

        try
        {
          var dataBuffer = await SubscribeToServer(connection, session, cancellationToken);
          var serverContext = new ServerContext(metadata ?? new HttpRequestMetadata());

          var rawResult = await MethodInvokingFactory.GetInvokingStrategy(methodDescription)
            .Invoke(methodInfo, methodHandler, serverContext, dataBuffer, cancellationToken);

          var resultBytes = await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
            .HandleResult(rawResult, cancellationToken);

          await SendResultToServer(connection, session, dataBuffer, serverContext, resultBytes, clientChannel, cancellationToken);
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

    private async Task<IContentBuffer> SubscribeToServer(HubConnection connection, Guid session, CancellationToken cancellationToken)
    {
      var channel = await connection.StreamAsChannelAsync<byte[]>("SubscribeToServer", session, cancellationToken);
      return _contentBufferFactory(channel.ReadAllAsync(cancellationToken));
    }

    private static async Task SendResultToServer(
      HubConnection connection, 
      Guid session, 
      IContentBuffer request, 
      ServerContext serverContext, 
      IAsyncEnumerable<byte[]> resultBytes,
      Channel<byte[]?> clientChannel,
      CancellationToken cancellationToken)
    {
      // TO DO:
      // 1. Await the first result byte to ensure that the client is ready to send the result to the server.
      var enumerator = resultBytes.GetAsyncEnumerator(cancellationToken);
      var hasItems = await enumerator.MoveNextAsync();

      // 2. Make sure the whole response is consumed
      await request.EnsureBuffered(cancellationToken);

      // 3. Form the response metadata
      var responseMetadata = serverContext.IsMetadataSet ? serverContext.Response : null;

      // 4. Send the GUID and metadata to the server and receive the server response channel.
      await connection.SendAsync("AddReadChannel", session, responseMetadata, clientChannel.Reader, cancellationToken);

      // 5. Send the first item to the server response channel and then send the rest of the items.
      if (hasItems)
      {
        do
        {
          await clientChannel.Writer.WriteAsync(enumerator.Current, cancellationToken);
        } while (await enumerator.MoveNextAsync());
      }

      clientChannel.Writer.TryComplete();
    }
  }
}
