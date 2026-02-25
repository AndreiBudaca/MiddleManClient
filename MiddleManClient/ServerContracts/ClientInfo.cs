namespace MiddleManClient.ServerContracts
{
  public class ClientInfo
  {
    public int Version { get; set; } = 0;

    public bool SupportsStreaming { get; set; } = true;

    public bool SendHTTPMetadata { get; set; }

    public ClientInfo() { }
    public ClientInfo(bool sendHttpMetadata)
    {
      SendHTTPMetadata = sendHttpMetadata;
    }
  }
}