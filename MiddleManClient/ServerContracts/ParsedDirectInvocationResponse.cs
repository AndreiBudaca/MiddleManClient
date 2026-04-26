namespace MiddleManClient.ServerContracts
{
  public class ParsedDirectInvocationResponse<T>
  {
    public HttpResponseMetadata? Metadata { get; set; }
    public T? Data { get; set; }
  }
}