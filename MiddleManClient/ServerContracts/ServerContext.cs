namespace MiddleManClient.ServerContracts
{
  public class ServerContext
  {
    public HttpRequestMetadata Request { get; }

    public HttpResponseMetadata Response { get; } = new HttpResponseMetadata();

    public ServerContext(HttpRequestMetadata requestMetadata)
    {
      Request = requestMetadata;
    }
  }
}
