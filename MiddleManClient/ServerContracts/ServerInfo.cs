namespace MiddleManClient.ServerContracts
{
  public class ServerInfo
  {
    public bool IsAccepted { get; set; }

    public int MaxMessageLength { get; set; }

    public byte[]? MethodSignature { get; set; }
  }
}
