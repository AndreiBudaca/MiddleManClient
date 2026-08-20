namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public interface IMethodResultHandlingStrategy
  {
    public Task<IAsyncEnumerable<byte[]>> HandleResult(object? result, CancellationToken cancellationToken);
  }
}
