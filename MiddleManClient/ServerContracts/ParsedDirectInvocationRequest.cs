namespace MiddleManClient.ServerContracts
{
  public class ParsedDirectInvocationRequest
  {
    public HttpRequestMetadata? Metadata { get; set; }
    public object[]? Data { get; set; }

    public DirectInvocationData ToDirectInvocationData()
    {
      var rawRequest = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Data);
      return new DirectInvocationData
      {
        Metadata = Metadata,
        Data = rawRequest
      };
    }
  }
}