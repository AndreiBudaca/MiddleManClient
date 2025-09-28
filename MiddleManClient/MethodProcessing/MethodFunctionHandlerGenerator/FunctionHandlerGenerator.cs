using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class FunctionHandlerGenerator : IMethodFunctionHandlerGenerator
  {
    public Func<byte[], Task<byte[]>> GenerateHandler(MethodInfo methodInfo, object? methodHandler)
    {
      if (!methodInfo.IsStatic && methodHandler == null)
      {
        throw new ArgumentNullException(nameof(methodHandler), "Method handler instance cannot be null for instance methods.");
      }

      return async (byte[] data) =>
      {
        var parameters = methodInfo.GetParameters();
        var args = Array.Empty<object?>();

        if (parameters.Length > 0)
        {
          var rawArgs = JsonSerializer.Deserialize<JsonArray>(data) ??
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

        if (result == null || result.GetType() == typeof(void))
        {
          return [];
        }

        return JsonSerializer.SerializeToUtf8Bytes(result);
      };
    }

    public static object? GetDefaultInstance(Type t)
    {
      if (!t.IsValueType || Nullable.GetUnderlyingType(t) != null)
      {
        return null;
      }

      return Activator.CreateInstance(t);
    }
  }
}
