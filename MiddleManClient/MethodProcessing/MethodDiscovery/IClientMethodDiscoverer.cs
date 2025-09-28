using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodDiscovery
{
  public interface IClientMethodDiscoverer
  {
    public IEnumerable<MethodInfo> Discover(Assembly? assembly);

    public static IClientMethodDiscoverer Default { get => new AttributeMethodDiscoverer(); }
  }
}
