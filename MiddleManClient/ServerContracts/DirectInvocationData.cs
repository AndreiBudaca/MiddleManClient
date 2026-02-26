namespace MiddleManClient.ServerContracts
{
  public class DirectInvocationData
  {
    public HttpRequestMetadata? Metadata { get; set; }
    public byte[] Data { get; set; } = [];
  }
}