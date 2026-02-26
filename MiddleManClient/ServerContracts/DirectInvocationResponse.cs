namespace MiddleManClient.ServerContracts
{
  public class DirectInvocationResponse
  {
    public HttpResponseMetadata? Metadata { get; set; }
    public byte[] Data { get; set; } = [];
  }
}