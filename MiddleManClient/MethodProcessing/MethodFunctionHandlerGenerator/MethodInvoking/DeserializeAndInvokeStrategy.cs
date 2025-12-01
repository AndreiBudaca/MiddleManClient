using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class DeserializeAndInvokeStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ChannelReader<byte[]> serverChannel)
    {
      var serverData = await ReadServerDataAsync(serverChannel);
      var parameters = methodInfo.GetParameters();
      var args = Array.Empty<object?>();

      if (parameters.Length > 0)
      {
        var rawArgs = JsonSerializer.Deserialize<JsonArray>(serverData) ??
         throw new InvalidOperationException($"Cannot process input data");

        args = Enumerable.Range(0, parameters.Length)
          .Select(i =>
          {
            var rawArg = i < rawArgs.Count ? rawArgs[i] : null;
            var parameter = parameters[i];

            return rawArg?.Deserialize(parameter.ParameterType) ?? GetDefaultInstance(parameter.ParameterType);
          })
          .ToArray();
      }

      return methodInfo.Invoke(methodHandler, args);
    }

    private static async Task<Stream> ReadServerDataAsync(ChannelReader<byte[]> serverChannel)
    {
      var dataStream = new MemoryStream();
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
