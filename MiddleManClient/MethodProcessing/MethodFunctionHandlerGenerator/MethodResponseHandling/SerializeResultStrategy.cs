using System.Text.Json;
using MiddleManClient.Extensions;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling
{
  public class SerializeResultStrategy : IMethodResultHandlingStrategy
  {
    public async Task<IAsyncEnumerable<byte[]>> HandleResult(object? result, CancellationToken cancellationToken)
    {
      var awaitedResult = await result.TryAwait(cancellationToken).ConfigureAwait(false);
      return JsonSerializer.SerializeToUtf8Bytes(awaitedResult).AsAsyncEnumerable();
    }
  }
}
