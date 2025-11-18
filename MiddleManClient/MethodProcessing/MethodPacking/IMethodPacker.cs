using MiddleManClient.MethodProcessing.Models;

namespace MiddleManClient.MethodProcessing.MethodPacking
{
  public interface IMethodPacker
  {
    public byte[]? GetDiff(byte[]? serverData, List<WebSocketClientMethod> knownMethods);

    public static IMethodPacker Default { get => new MethodPacker(); }
  }
}
