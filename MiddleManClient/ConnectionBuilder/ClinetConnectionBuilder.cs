using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.ConnectionBuilder.Exceptions;

namespace MiddleManClient.ConnectionBuilder
{
  internal class ClientConnectionBuilder : IClientConnectionBuilder
  {
    private string _host = "localhost";
    private string _token = string.Empty;
    private bool _reconnect = false;

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

      var hubConnectionBuilder = new HubConnectionBuilder()
        .WithUrl(_host, options =>
        {
          options.Headers.Add("Authorization", $"Bearer {_token}");
        });

      if (_reconnect)
      {
        hubConnectionBuilder = hubConnectionBuilder.WithAutomaticReconnect();
      }

      return new ClientConnection(hubConnectionBuilder.Build());
    }
  }
}
