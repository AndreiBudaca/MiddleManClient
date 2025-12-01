using MiddleManClient.MethodProcessing.Models;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class MethodResultHandlingFactory
  {
    public static IMethodResultHandlingStrategy GetResultHandlingStrategy(WebSocketClientMethod methodData)
    {
      if (methodData.Returns?.IsBinary ?? false)
      {
        return new SendRawResponseStrategy();
      }

      return new SerializeAndSendResultStrategy();
    }
  }
}
