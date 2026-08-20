using MiddleManClient.Extensions;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class KeepRawResponseStrategy : IMethodResultHandlingStrategy
  {
    public async Task<IAsyncEnumerable<byte[]>> HandleResult(object? result, CancellationToken cancellationToken)
    {
      var awaitedResult = await result.TryAwait(cancellationToken).ConfigureAwait(false);

      if (awaitedResult is byte[] bytes)
      {
        return bytes.AsAsyncEnumerable();
      }

      if (awaitedResult is IAsyncEnumerable<byte[]> asyncEnumerable)
      {
        return asyncEnumerable;
      }

      throw new InvalidOperationException($"Cannot handle result of type {awaitedResult?.GetType().FullName ?? "null"} as raw response.");
    }
  }
}
