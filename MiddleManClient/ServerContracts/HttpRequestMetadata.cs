namespace MiddleManClient.ServerContracts
{
  public class HttpRequestMetadata
  {
    public string Method { get; set; } = "GET";

    public string Path { get; set; } = "/";

    public List<HttpHeader> Headers { get; set; } = [];
  }
}
