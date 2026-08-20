using MiddleManClient.Buffer;
using MiddleManClient.ServerContracts;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class DeserializeAndInvokeStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ServerContext context, IContentBuffer content, CancellationToken cancellationToken = default)
    {
      var rawArgs = await ReadServerStreamDataAsync(content, cancellationToken);
      return InvokeWithArgs(methodInfo, methodHandler, rawArgs, context);
    }

    private static object? InvokeWithArgs(MethodInfo methodInfo, object? methodHandler, JsonArray rawArgs, ServerContext context)
    {
      var parameters = methodInfo.GetParameters();

      if (parameters.Length == 0) return InvokeWithArgs(methodInfo, methodHandler, []);

      var args = new object?[parameters.Length];
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

      return InvokeWithArgs(methodInfo, methodHandler, args);
    }

    private static object? InvokeWithArgs(MethodInfo methodInfo, object? methodHandler, object?[] args)
    {
      return methodInfo.Invoke(methodHandler, args);
    }

    private static async Task<JsonArray> ReadServerStreamDataAsync(IContentBuffer content, CancellationToken cancellationToken)
    {
      var dataStream = new MemoryStream();

      await foreach (var serverData in content.Read(cancellationToken))
      {
        await dataStream.WriteAsync(serverData, cancellationToken);
      }

      dataStream.Position = 0;
      if (dataStream.Length == 0)
      {
        return [];
      }

      return JsonSerializer.Deserialize<JsonArray>(dataStream) ??
         throw new InvalidOperationException($"Cannot process input data");
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
