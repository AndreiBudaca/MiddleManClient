using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.Extensions;
using MiddleManClient.MethodProcessing.MethodDiscovery;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator;
using MiddleManClient.MethodProcessing.MethodPacking;
using MiddleManClient.MethodProcessing.MethodParsing;
using MiddleManClient.MethodProcessing.Models;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient
{
  public class ClientConnection(HubConnection[] connections)
  {
    private readonly HubConnection[] _connections = connections;
    private readonly List<WebSocketClientMethod> _knownMethods = [];
    private readonly Dictionary<Type, object> _methodCallingHandler = [];
    private Assembly? _assembly;

    private ServerInfo? _serverInfo;
    private readonly ClientInfo info = new();
    private TimeSpan _invocationTimeout = TimeSpan.FromMinutes(2);

    private IClientMethodDiscoverer _methodDiscoverer = IClientMethodDiscoverer.Default;
    private IMethodFunctionHandlerGenerator _handlerGenerator = IMethodFunctionHandlerGenerator.Default;
    private IMethodParser _methodParser = IMethodParser.Default;
    private IMethodPacker _methodPacker = IMethodPacker.Default;

    public ClientConnection RequestHttpMetadata(bool value)
    {
      info.SendHTTPMetadata = value;
      return this;
    }

    public ClientConnection WithInvocationTimeout(TimeSpan timeout)
    {
      _invocationTimeout = timeout;
      return this;
    }

    public ClientConnection UseAssembly(Assembly assembly)
    {
      _assembly = assembly;
      return this;
    }

    public ClientConnection UseMethodDiscovery(IClientMethodDiscoverer discoverer)
    {
      _methodDiscoverer = discoverer;
      return this;
    }

    public ClientConnection UseMethodFunctionHandlerGenerator(IMethodFunctionHandlerGenerator generator)
    {
      _handlerGenerator = generator;
      info.SupportsStreaming = generator.SupportsStreaming;

      return this;
    }

    public ClientConnection UseMethodParser(IMethodParser parser)
    {
      _methodParser = parser;
      return this;
    }

    public ClientConnection AddMethodCallingHandler<T>(T handler) where T : class
    {
      _methodCallingHandler[typeof(T)] = handler;
      return this;
    }

    public async Task StartAsync()
    {
      if (_connections.Length == 0) throw new InvalidOperationException("At least one connection must be provided");

      var methods = _methodDiscoverer.Discover(_assembly);

      foreach (var connection in _connections)
      {
        connection.Reconnected += async arg =>
        {
          Console.WriteLine("Reconnected to server, renegociating...");
          _serverInfo = await connection.InvokeAsync<ServerInfo>("Negociate", info);
          if (!_serverInfo.IsAccepted) throw new Exception("Connection rejected by server");
        };
      }

      await Task.WhenAll(_connections.Select(async connection =>
      {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await connection.StartAsync(cts.Token);
        Console.WriteLine("Connected to server, negotiating...");
        _serverInfo = await connection.InvokeAsync<ServerInfo>("Negociate", info);
        if (!_serverInfo.IsAccepted) throw new Exception("Connection rejected by server");
      }));

      foreach (var method in methods)
      {
        var parsedMethod = _methodParser.Parse(method);
        _knownMethods.Add(parsedMethod);

        _methodCallingHandler.TryGetValue(method.DeclaringType!, out var handlerInstance);

        foreach (var connection in _connections)
        {
          _handlerGenerator.GenerateHandler(connection, method, parsedMethod, handlerInstance, _serverInfo?.MaxMessageLength ?? 4096, _invocationTimeout);
        }
      }

      var diff = _methodPacker.GetDiff(_serverInfo?.MethodSignature, _knownMethods);
      if (diff != null && diff.Length > 0)
      {
        await _connections[0].SendChunksAsync("Methods", _serverInfo?.MaxMessageLength ?? 4096, diff);
      }

      // Infinite wait to keep the connection alive
      var infiniteLock = new object();
      lock (infiniteLock)
      {
        Monitor.Wait(infiniteLock);
      }
    }
  }
}
