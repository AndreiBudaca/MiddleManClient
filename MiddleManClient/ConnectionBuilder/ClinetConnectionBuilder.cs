using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MiddleManClient.ConnectionBuilder.Exceptions;

namespace MiddleManClient.ConnectionBuilder
{
  internal class ClientConnectionBuilder : IClientConnectionBuilder
  {
    private string _host = "localhost";
    private string _token = string.Empty;
    private bool _reconnect = false;
    private int _poolSize = 1;

    public IClientConnectionBuilder WithHost(string host)
    {
      _host = host;
      return this;
    }

    public IClientConnectionBuilder WithReconnect()
    {
      _reconnect = true;
      return this;
    }

    public IClientConnectionBuilder WithToken(string token)
    {
      _token = token;
      return this;
    }

    public IClientConnectionBuilder WithPoolSize(int poolSize)
    {
      if (poolSize <= 0)
      {
        throw new InvalidClientOptionsException("Pool size must be greater than 0");
      }

      _poolSize = poolSize;
      return this;
    }

    public ClientConnection Build()
    {
      var connections = new HubConnection[_poolSize];
      for (int i = 0; i < _poolSize; i++)
      {
        connections[i] = BuildHubConnection();
      }

      return new ClientConnection(connections);
    }

    private HubConnection BuildHubConnection()
    {
      var identity = Guid.NewGuid();

      if (string.IsNullOrWhiteSpace(_host))
      {
        throw new InvalidClientOptionsException("Host cannot be null or empty");
      }

      IHubConnectionBuilder hubConnectionBuilder = new HubConnectionBuilder();

      if (!string.IsNullOrEmpty(_token))
      {
        hubConnectionBuilder = hubConnectionBuilder.WithUrl(_host, options =>
        {
          options.Headers.Add("Authorization", $"Bearer {_token}");
          options.Headers.Add("X-Client-Identity", identity.ToString());
          options.SkipNegotiation = true;
          options.Transports = HttpTransportType.WebSockets;
        });
      }

      if (_reconnect)
      {
        hubConnectionBuilder = hubConnectionBuilder.WithAutomaticReconnect();
      }

      return hubConnectionBuilder
        .AddMessagePackProtocol()
        .WithKeepAliveInterval(TimeSpan.FromSeconds(15))
        .Build();
    }
  }
}
