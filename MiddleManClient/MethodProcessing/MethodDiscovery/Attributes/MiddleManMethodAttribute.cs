namespace MiddleManClient.MethodProcessing.MethodDiscovery.Attributes
{
  [AttributeUsage(AttributeTargets.Method)]
  public class MiddleManMethodAttribute : Attribute
  {
    public bool RawData { get; set; } = false;
  }
}
