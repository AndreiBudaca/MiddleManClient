using MiddleManClient.Buffer;
using MiddleManClient.Extensions;
using MiddleManClient.ServerContracts;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.Parsers
{
  public class ServerContextParser
  {
    private const int MaxMetadataBytes = 64 * 1024;

    public static async Task<(ServerContext, IContentBuffer)> ParseServerContextFromStream(ChannelReader<byte[]> channelReader, Func<IAsyncEnumerable<byte[]>, IContentBuffer> contentBufferFactory, CancellationToken cancellationToken = default)
    {
      var defaultRequestMetadata = new HttpRequestMetadata();

      var serverDataEnumerable = channelReader.ReadAllAsync(cancellationToken);

      var metadataLenghtEnumerationResult = await serverDataEnumerable.EnumerateUntil(4, 0, cancellationToken);

      var metadataLength = BitConverter.ToInt32(metadataLenghtEnumerationResult.Received);

      if (metadataLength < 0 || metadataLength > MaxMetadataBytes)
      {
        throw new InvalidDataException($"Invalid metadata length: {metadataLength}.");
      }

      if (metadataLength == 0)
      {
        return new(new(defaultRequestMetadata), contentBufferFactory(metadataLenghtEnumerationResult.Next));
      }

      var metadataBytesEnumerationResult = await metadataLenghtEnumerationResult.Next.EnumerateUntil(metadataLength, 0, cancellationToken);

      var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytesEnumerationResult.Received);
      var requestMetadata = System.Text.Json.JsonSerializer.Deserialize<HttpRequestMetadata>(metadataJson);

      var serverContext = new ServerContext(requestMetadata ?? defaultRequestMetadata);
      return (serverContext, contentBufferFactory(metadataBytesEnumerationResult.Next));
    }

    public static (ServerContext, int bufferOffet) ParseServerContextFromBuffer(byte[] data)
    {
      var defaultRequestMetadata = new HttpRequestMetadata();
      if (data.Length < 4) return (new ServerContext(defaultRequestMetadata), 0);

      var metadataLength = BitConverter.ToInt32(data, 0);
      if (metadataLength < 0 || metadataLength > MaxMetadataBytes)
      {
        throw new InvalidDataException($"Invalid metadata length: {metadataLength}.");
      }

      if (metadataLength == 0) return (new ServerContext(defaultRequestMetadata), 4);

      if (data.Length < 4 + metadataLength)
      {
        throw new InvalidDataException($"Invalid metadata payload. Expected {metadataLength} bytes, got {data.Length - 4}.");
      }

      var metadataJson = System.Text.Encoding.UTF8.GetString(data, 4, metadataLength);
      var requestMetadata = System.Text.Json.JsonSerializer.Deserialize<HttpRequestMetadata>(metadataJson);

      return (new ServerContext(requestMetadata ?? defaultRequestMetadata), 4 + metadataLength);
    }
  }
}
