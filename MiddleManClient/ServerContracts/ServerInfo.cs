namespace MiddleManClient.ServerContracts
{
  public class ServerInfo
  {
    public int MaxMessageLength { get; set; }

    public byte[]? MethodSignature { get; set; }

    public int[] AcceptedVersions { get; set; } = [];
  }
}
