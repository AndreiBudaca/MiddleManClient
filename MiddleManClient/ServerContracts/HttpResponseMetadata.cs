namespace MiddleManClient.ServerContracts
{
  public class HttpResponseMetadata
  {
    public int ResponseCode { get; set; } = 200;

    public List<HttpHeader> Headers { get; set; } = [];

    public byte[] SerializeJson()
    {
      var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(this);
      var metadataLength = BitConverter.GetBytes(jsonBytes.Length);

      return metadataLength.Concat(jsonBytes).ToArray();
    }
  }
}
