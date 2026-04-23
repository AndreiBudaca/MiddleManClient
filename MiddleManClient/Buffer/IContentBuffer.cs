namespace MiddleManClient.Buffer
{
  public interface IContentBuffer : IAsyncDisposable
  {
    public IAsyncEnumerable<byte[]> Read(CancellationToken cancellationToken);
  }
}