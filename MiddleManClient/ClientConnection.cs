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

    private Assembly? _assembly;
    private IClientMethodDiscoverer? _methodDiscoverer;
    private IMethodFunctionHandlerGenerator? _handlerGenerator;
    private IMethodParser? _methodParser;
    private IMethodPacker? _methodPacker;

    public void UseMethodDiscovery(IClientMethodDiscoverer discoverer)
    {
      _methodDiscoverer = discoverer;
    }

    public void UseMethodFunctionHandlerGenerator(IMethodFunctionHandlerGenerator generator)
    {
      _handlerGenerator = generator;
    }

    public void UseMethodParser(IMethodParser parser)
    {
      _methodParser = parser;
    }

    public void UseAssembly(Assembly assembly)
    {
      _assembly = assembly;
    }

    public void AddMethodCallingHandler<T>(T handler) where T : class
    {
      _methodCallingHandler[typeof(T)] = handler;
    }

    public async Task StartAsync()
    {
      _methodDiscoverer ??= IClientMethodDiscoverer.Default;
      _methodParser ??= IMethodParser.Default;
      _handlerGenerator ??= IMethodFunctionHandlerGenerator.Default;
      _methodPacker ??= IMethodPacker.Default;

      var methods = _methodDiscoverer.Discover(_assembly);

      await _connection.StartAsync();

      _serverInfo = await _connection.InvokeAsync<ServerInfo>("ServerInfo");

      foreach (var method in methods)
      {
        var parsedMethod = _methodParser.Parse(method);
        _knownMethods.Add(parsedMethod);

        _methodCallingHandler.TryGetValue(method.DeclaringType!, out var handlerInstance);
        var functionHandler = _handlerGenerator.GenerateHandler(_connection, method, parsedMethod, handlerInstance, _serverInfo?.MaxMessageLength ?? 4096);
        _connection.On(parsedMethod.Name, functionHandler);
      }

      var diff = _methodPacker.GetDiff(_serverInfo?.MethodSignature, _knownMethods);
      if (diff != null && diff.Length > 0)
      {
        await _connection.SendChunksAsync("Methods", _serverInfo?.MaxMessageLength ?? 4096, diff);
      }
    }
  }
}
