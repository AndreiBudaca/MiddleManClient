namespace MiddleManClient.ConnectionBuilder
{
  public interface IClientConnectionBuilder
  {
    public IClientConnectionBuilder WithHost(string host);

    public IClientConnectionBuilder WithToken(string token);

    public IClientConnectionBuilder WithReconnect();

    public ClientConnection Build();
  }
}
