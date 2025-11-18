using MiddleManClient.MethodProcessing.Models;
using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodParsing
{
  public interface IMethodParser
  {
    public WebSocketClientMethod Parse(MethodInfo methodInfo);

    public static IMethodParser Default { get => new MethodParser(); }
  }
}
