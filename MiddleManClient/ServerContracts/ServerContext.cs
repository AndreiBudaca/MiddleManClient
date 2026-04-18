namespace MiddleManClient.ServerContracts
{
  public class ServerContext(HttpRequestMetadata requestMetadata)
  {
    private HttpResponseMetadata _response = new HttpResponseMetadata();

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
