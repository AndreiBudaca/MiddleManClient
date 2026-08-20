namespace MiddleManClient.Extensions
{
  public static class ObjectExtensions
  {
    public static async Task<object?> TryAwait(this object? result, CancellationToken cancellationToken)
    {
      if (result is Task taskResult)
      {
        await taskResult.WaitAsync(cancellationToken).ConfigureAwait(false);
        var resultProperty = taskResult.GetType().GetProperty("Result");
        result = resultProperty?.GetValue(taskResult) ?? default;
      }

      return result;
    }
  }
}