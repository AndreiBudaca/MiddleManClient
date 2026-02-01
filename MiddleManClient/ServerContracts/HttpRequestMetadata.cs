namespace MiddleManClient.ServerContracts
{
  public class HttpRequestMetadata
  {
    public string Method { get; set; } = "GET";

    public string Path { get; set; } = "/";

    public HttpUser? User { get; set; }

    public List<HttpHeader> Headers { get; set; } = [];
  }
}
