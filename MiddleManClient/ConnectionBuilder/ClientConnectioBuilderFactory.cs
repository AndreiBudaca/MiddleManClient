namespace MiddleManClient.ConnectionBuilder
{
  public static class ClientConnectioBuilderFactory
  {
    public static IClientConnectionBuilder Create()
    {
      return new ClientConnectionBuilder();
    }
  }
}
