namespace MiddleManClient.ServerContracts
{
  public class ServerContext(HttpRequestMetadata requestMetadata)
  {
    private readonly HttpResponseMetadata _response = new();

    public HttpRequestMetadata Request { get; } = requestMetadata;

    public bool IsMetadataSet { get; private set; } = false;

    public HttpResponseMetadata Response 
    { 
      get
      {
        IsMetadataSet = true;
        return _response;
      }
    }
  }
}
