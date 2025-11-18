using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.Extensions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class FunctionHandlerGenerator : IMethodFunctionHandlerGenerator
  {
    public Func<Guid, Task> GenerateHandler(HubConnection connection, MethodInfo methodInfo, object? methodHandler, int maxMessageLength)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      return async (Guid session) =>
      {
        var clientChannel = Channel.CreateUnbounded<byte[]>();
        var serverChannel = await connection.StreamAsChannelAsync<byte[]>("SubscribeToServer", session);
        await connection.SendAsync("AddReadChannel", session, clientChannel.Reader);
        object? result = null;

        try
        {
          var serverData = await ReadServerDataAsync(serverChannel);
          result = await InvokeMethod(methodInfo, methodHandler, serverData);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
        }
        finally
        {
          await WriteMethodResultAsync(clientChannel.Writer, result, maxMessageLength);
        }
      };
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

    private static async Task WriteMethodResultAsync(ChannelWriter<byte[]> writer, object? methodResult, int maxMessageLength)
    {
      var data = methodResult != null ? JsonSerializer.SerializeToUtf8Bytes(methodResult) : [];

      await writer.WriteChunkedData(maxMessageLength, data);
      writer.Complete();
    }

    private async Task<object?> InvokeMethod(MethodInfo methodInfo, object? methodHandler, Stream serverData)
    {
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

      var result = methodInfo.Invoke(methodHandler, args);

      if (result is Task taskResult)
      {
        await taskResult.ConfigureAwait(false);
        var resultProperty = taskResult.GetType().GetProperty("Result");
        result = resultProperty?.GetValue(taskResult) ?? default;
      }

      return result;
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
