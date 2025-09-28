using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public interface IMethodFunctionHandlerGenerator
  {
    public Func<byte[], Task<byte[]>> GenerateHandler(MethodInfo methodInfo, object? methodHandler);

    public static IMethodFunctionHandlerGenerator Default { get => new FunctionHandlerGenerator(); }
  }
}
