using Microsoft.AspNetCore.SignalR.Client;
using MiddleManClient.MethodProcessing.MethodDiscovery;
using MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator;
using MiddleManClient.MethodProcessing.MethodParsing;
using MiddleManClient.ServerContracts;
using System.Reflection;

namespace MiddleManClient
{
  public class ClientConnection(HubConnection connection)
  {
    private readonly HubConnection _connection = connection;
    private readonly List<WebSocketClientMethod> _knownMethods = [];
    private readonly Dictionary<Type, object> _methodCallingHandler = [];

    private Assembly? _assembly;
    private IClientMethodDiscoverer? _methodDiscoverer;
    private IMethodFunctionHandlerGenerator? _handlerGenerator;
    private IMethodParser? _methodParser;

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

      var methods = _methodDiscoverer.Discover(_assembly);

      foreach (var method in methods)
      {
        var parsedMethod = _methodParser.Parse(method);
        _knownMethods.Add(parsedMethod);
       
        _methodCallingHandler.TryGetValue(method.DeclaringType!, out var handlerInstance);
        var functionHandler = _handlerGenerator.GenerateHandler(method, handlerInstance);
        _connection.On(parsedMethod.Name, functionHandler);
      }

      _connection.Reconnected += async (string? arg) =>
      {
        await _connection.InvokeAsync("AddMethodInfo", _knownMethods);
      };

      await _connection.StartAsync();
      await _connection.InvokeAsync("AddMethodInfo", _knownMethods);
    }
  }
}
