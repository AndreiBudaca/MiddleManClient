using System.Reflection;

namespace MiddleManClient.MethodProcessing.MethodDiscovery
{
  public class AttributeMethodDiscoverer : IClientMethodDiscoverer
  {
    public IEnumerable<MethodInfo> Discover(Assembly? assembly)
    {
      assembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

      var methods = assembly.GetTypes()
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        .Where(m => m.GetCustomAttributes(typeof(Attributes.MiddleManMethodAttribute), false).Length > 0);

      return methods;
    }
  }
}
