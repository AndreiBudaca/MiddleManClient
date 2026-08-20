namespace MiddleManClient.Buffer
{
  public interface IContentBuffer : IAsyncDisposable
  {
    public bool IsCompleted { get; }

    public IAsyncEnumerable<byte[]> Read(CancellationToken cancellationToken);
  }
}