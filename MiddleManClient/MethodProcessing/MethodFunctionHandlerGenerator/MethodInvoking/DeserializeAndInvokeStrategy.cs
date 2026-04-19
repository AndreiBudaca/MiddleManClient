using MiddleManClient.ServerContracts;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class DeserializeAndInvokeStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ChannelReader<byte[]> serverChannel, ServerContext context, byte[] additionalItem, CancellationToken cancellationToken = default)
    {
      var rawArgs = await ReadServerStreamDataAsync(serverChannel, additionalItem, cancellationToken);
      return InvokeWithArgs(methodInfo, methodHandler, rawArgs, context);
    }

    public object? Invoke(MethodInfo methodInfo, object? methodHandler, byte[] serverData, ServerContext context)
    {
      var rawArgs = (serverData.Length > 0 ? JsonSerializer.Deserialize<JsonArray>(serverData) : [])
       ?? throw new InvalidOperationException($"Cannot process input data");
       
      return InvokeWithArgs(methodInfo, methodHandler, rawArgs, context);
    }

    private static object? InvokeWithArgs(MethodInfo methodInfo, object? methodHandler, JsonArray rawArgs, ServerContext context)
    {
      var parameters = methodInfo.GetParameters();
      var args = new object?[parameters.Length];

      if (parameters.Length > 0)
      {
        int rawArgPos = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
          if (parameters[i].ParameterType == typeof(ServerContext))
          {
            args[i] = context;
          }
          else
          {
            var rawArg = rawArgPos < rawArgs.Count ? rawArgs[rawArgPos] : null;
            args[i] = rawArg?.Deserialize(parameters[i].ParameterType) ?? GetDefaultInstance(parameters[i].ParameterType);
            ++rawArgPos;
          }
        }
      }

      return methodInfo.Invoke(methodHandler, args);
    }

    private static async Task<JsonArray> ReadServerStreamDataAsync(ChannelReader<byte[]> serverChannel, byte[] additionalItem, CancellationToken cancellationToken)
    {
      var dataStream = new MemoryStream();
      dataStream.Write(additionalItem, 0, additionalItem.Length);

      await foreach (var serverData in serverChannel.ReadAllAsync().WithCancellation(cancellationToken))
      {
        await dataStream.WriteAsync(serverData, cancellationToken);
      }

      dataStream.Position = 0;
      return JsonSerializer.Deserialize<JsonArray>(dataStream) ??
         throw new InvalidOperationException($"Cannot process input data");;
    }

    private static object? GetDefaultInstance(Type t)
    {
      if (!t.IsValueType || Nullable.GetUnderlyingType(t) != null)
      {
        return null;
      }

      return Activator.CreateInstance(t);
    }
  }
}
