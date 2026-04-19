using MiddleMan.Core.Extensions;
using MiddleManClient.ServerContracts;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class ServerContextParser
  {
    private const int MaxMetadataBytes = 64 * 1024;

    public static async Task<(ServerContext, byte[] currentItem)> ParseServerContextFromStream(ChannelReader<byte[]> channelReader, CancellationToken cancellationToken = default)
    {
      var defaultRequestMetadata = new HttpRequestMetadata();

      var serverDataEnumerable = channelReader.ReadAllAsync();

      var metadataLengthBytes = await serverDataEnumerable.EnumerateUntil(4, 0, cancellationToken);
      var metadataLength = BitConverter.ToInt32(metadataLengthBytes.Received);

      if (metadataLength < 0 || metadataLength > MaxMetadataBytes)
      {
        throw new InvalidDataException($"Invalid metadata length: {metadataLength}.");
      }

      if (metadataLength == 0)
      {
        return new(new(defaultRequestMetadata), metadataLengthBytes.CurrentEnumerationItem);
      }

      var metadataBytes = await serverDataEnumerable
        .PrependItems(metadataLengthBytes.CurrentEnumerationItem)
        .EnumerateUntil(metadataLength, 0, cancellationToken);

      var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytes.Received);
      var requestMetadata = System.Text.Json.JsonSerializer.Deserialize<HttpRequestMetadata>(metadataJson);

      var serverContext = new ServerContext(requestMetadata ?? defaultRequestMetadata);
      return (serverContext, metadataBytes.CurrentEnumerationItem);
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
