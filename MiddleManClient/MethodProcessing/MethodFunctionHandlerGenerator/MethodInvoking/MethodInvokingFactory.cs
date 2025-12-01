using MiddleManClient.MethodProcessing.Models;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodInvoking
{
  public class MethodInvokingFactory
  {
    public static IMethodInvokingStrategy GetInvokingStrategy(WebSocketClientMethod methodData)
    {
      if (methodData.Arguments.Any(x => x.IsBinary))
      {
        return new InvokeWithRawDataStrategy();
      }

      return new DeserializeAndInvokeStrategy();
    }
  }
}
