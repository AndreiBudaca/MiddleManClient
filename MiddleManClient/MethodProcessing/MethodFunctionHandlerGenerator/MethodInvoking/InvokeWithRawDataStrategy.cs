using MiddleManClient.Buffer;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class InvokeWithRawDataStrategy : IMethodInvokingStrategy
  {
    public async Task<object?> Invoke(MethodInfo methodInfo, object? methodHandler, ServerContext context, IContentBuffer content, CancellationToken cancellationToken = default)
    {
      var (hasContext, contextPosition) = GetContextParameterInfo(methodInfo.GetParameters());
      var data = content.Read(cancellationToken);

      if (hasContext)
      {
        return methodInfo.Invoke(methodHandler, contextPosition == 0 ? [context, data] : [data, context]);
      }

      return methodInfo.Invoke(methodHandler, [data]);
    }

    public object? Invoke(MethodInfo methodInfo, object? methodHandler, byte[] serverData, ServerContext context)
    {
      var (hasContext, contextPosition) = GetContextParameterInfo(methodInfo.GetParameters());
      
      if (hasContext)
      {
        return methodInfo.Invoke(methodHandler, contextPosition == 0 ? [context, serverData] : [serverData, context]);
      }

      return methodInfo.Invoke(methodHandler, [serverData]);
    }

    private static (bool hasContext, int contextPosition) GetContextParameterInfo(ParameterInfo[] parameters)
    {
      for (int i = 0; i < parameters.Length; i++)
      {
        if (parameters[i].ParameterType == typeof(ServerContext))
        {
          return (true, i);
        }
      }

      return (false, -1);
    }
  }
}
