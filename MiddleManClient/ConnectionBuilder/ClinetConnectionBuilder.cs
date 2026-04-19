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
    private readonly Guid _identity = Guid.NewGuid();


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

    public ClientConnection Build()
    {
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
          options.Headers.Add("X-Client-Identity", _identity.ToString());
          options.SkipNegotiation = true;
          options.Transports = HttpTransportType.WebSockets;
        });
      }

      if (_reconnect)
      {
        hubConnectionBuilder = hubConnectionBuilder.WithAutomaticReconnect();
      }

      return new ClientConnection(hubConnectionBuilder
        .AddMessagePackProtocol()
        .WithKeepAliveInterval(TimeSpan.FromSeconds(15))
        .Build()
      );
    }
  }
}
