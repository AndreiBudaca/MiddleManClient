namespace MiddleManClient.MethodProcessing.Models
{
  public class WebSocketClientMethod
  {
    public required string Name { get; set; }

    public List<WebSocketClientMethodArgument> Arguments { get; set; } = [];

    public WebSocketClientMethodArgument? Returns { get; set; }
  }
}
