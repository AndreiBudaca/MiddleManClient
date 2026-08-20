using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.Buffer;
using MiddleManClient.Extensions;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling;
using MiddleManClient.MethodProcessing.Models;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class DirectInvocationFunctionHandlerGenerator(Func<IAsyncEnumerable<byte[]>, IContentBuffer> contentBufferFactory) : IMethodFunctionHandlerGenerator
  {
    private readonly Func<IAsyncEnumerable<byte[]>, IContentBuffer> _contentBufferFactory = contentBufferFactory;
    
    public bool SupportsStreaming => false;

    public DirectInvocationFunctionHandlerGenerator() : this(content => new MemoryBuffer(content, int.MaxValue)) { }

    public void GenerateHandler(HubConnection connection, MethodInfo methodInfo, WebSocketClientMethod methodDescription, object? methodHandler, int maxMessageLength, TimeSpan timeout)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      connection.On<DirectInvocationData, DirectInvocationResponse>(methodDescription.Name, async data =>
      {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var cancellationToken = timeoutCts.Token;

        var dataBuffer = WrapServerData(data.Data);
        var serverContext = new ServerContext(data.Metadata ?? new HttpRequestMetadata());

        var rawResult = await MethodInvokingFactory.GetInvokingStrategy(methodDescription)
          .Invoke(methodInfo, methodHandler, serverContext, dataBuffer, cancellationToken);

        var resultBytes = await MethodResultHandlingFactory.GetResultHandlingStrategy(methodDescription)
          .HandleResult(rawResult, cancellationToken);

        return new DirectInvocationResponse
        {
          Metadata = serverContext.IsMetadataSet ? serverContext.Response : null,
          Data = await resultBytes.ReadAllBytes(maxMessageLength, cancellationToken)
        };
      });
    }

    private IContentBuffer WrapServerData(byte[] serverData)
    {
      return _contentBufferFactory(serverData.AsAsyncEnumerable());
    }
  }
}
