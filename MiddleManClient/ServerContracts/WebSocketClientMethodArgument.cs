namespace MiddleManClient.ServerContracts
{
  public class WebSocketClientMethodArgument
  {
    public string? Name { get; set; }

    public bool IsPrimitive { get; set; }

    public bool IsArray {  get; set; }

    public bool IsNullable {  get; set; }

    public bool IsNumeric { get; set; }

    public bool IsBoolean { get; set; }

    public string? Type { get; set; }

    public List<WebSocketClientMethodArgument> Components { get; set; } = [];
  }
}
