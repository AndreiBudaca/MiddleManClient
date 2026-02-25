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
  public class ClientConnection(HubConnection connection)
  {
    private readonly HubConnection _connection = connection;
    private readonly List<WebSocketClientMethod> _knownMethods = [];
    private readonly Dictionary<Type, object> _methodCallingHandler = [];
    private ServerInfo? _serverInfo;

    private readonly ClientInfo info = new();
    private Assembly? _assembly;
    private IClientMethodDiscoverer _methodDiscoverer = IClientMethodDiscoverer.Default;
    private IMethodFunctionHandlerGenerator _handlerGenerator = IMethodFunctionHandlerGenerator.Default;
    private IMethodParser _methodParser = IMethodParser.Default;
    private IMethodPacker _methodPacker = IMethodPacker.Default;

    public ClientConnection RequestHttpMetadata(bool value)
    {
      info.SendHTTPMetadata = value;
      return this;
    }

    public ClientConnection UseStreaming(bool value)
    {
      info.SupportsStreaming = value;
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
      var methods = _methodDiscoverer.Discover(_assembly);

      await _connection.StartAsync();

      _serverInfo = await _connection.InvokeAsync<ServerInfo>("Negociate", info);
      if (!_serverInfo.IsAccepted) throw new Exception("Connection rejected by server");

      foreach (var method in methods)
      {
        var parsedMethod = _methodParser.Parse(method);
        _knownMethods.Add(parsedMethod);

        _methodCallingHandler.TryGetValue(method.DeclaringType!, out var handlerInstance);
        _handlerGenerator.GenerateHandler(_connection, method, parsedMethod, handlerInstance, _serverInfo?.MaxMessageLength ?? 4096);
      }

      var diff = _methodPacker.GetDiff(_serverInfo?.MethodSignature, _knownMethods);
      if (diff != null && diff.Length > 0)
      {
        await _connection.SendChunksAsync("Methods", _serverInfo?.MaxMessageLength ?? 4096, diff);
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
