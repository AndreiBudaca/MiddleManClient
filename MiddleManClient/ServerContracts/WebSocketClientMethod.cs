namespace MiddleManClient.ServerContracts
{
  public class WebSocketClientMethod
  {
    public required string Name { get; set; }

    public List<WebSocketClientMethodArgument> Arguments { get; set; } = [];

    public WebSocketClientMethodArgument? Returns { get; set; }
  }
}
