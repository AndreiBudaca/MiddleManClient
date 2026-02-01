using MiddleManClient.ServerContracts;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class DeserializeAndInvokeStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ChannelReader<byte[]> serverChannel, ServerContext context, byte[] additionalItem)
    {
      var serverData = await ReadServerDataAsync(serverChannel, additionalItem);
      var parameters = methodInfo.GetParameters();
      var args = new object?[parameters.Length];

      if (parameters.Length > 0)
      {
        var rawArgs = JsonSerializer.Deserialize<JsonArray>(serverData) ??
         throw new InvalidOperationException($"Cannot process input data");

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

    private static async Task<Stream> ReadServerDataAsync(ChannelReader<byte[]> serverChannel, byte[] additionalItem)
    {
      var dataStream = new MemoryStream();
      dataStream.Write(additionalItem, 0, additionalItem.Length);

      await foreach (var serverData in serverChannel.ReadAllAsync())
      {
        await dataStream.WriteAsync(serverData);
      }

      dataStream.Position = 0;
      return dataStream;
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
