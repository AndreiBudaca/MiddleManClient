using MiddleMan.Core.Extensions;
using MiddleManClient.ServerContracts;
using System.Threading.Channels;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator
{
  public class ServerContextParser
  {
    public static async Task<(ServerContext, byte[] currentItem)> ParseServerContext(ChannelReader<byte[]> channelReader)
    {
      var defaultRequestMetadata = new HttpRequestMetadata();

      var serverDataEnumerable = channelReader.ReadAllAsync();

      var metadataLengthBytes = await serverDataEnumerable.EnumerateUntil(4, 0);
      var metadataLength = BitConverter.ToInt32(metadataLengthBytes.Received);

      if (metadataLength == 0)
      {
        return new(new(defaultRequestMetadata), metadataLengthBytes.CurrentEnumerationItem);
      }

      var metadataBytes = await serverDataEnumerable
        .PrependItems(metadataLengthBytes.CurrentEnumerationItem)
        .EnumerateUntil(metadataLength, 0);

      var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytes.Received);
      var requestMetadata = System.Text.Json.JsonSerializer.Deserialize<HttpRequestMetadata>(metadataJson);

      var serverContext = new ServerContext(requestMetadata ?? defaultRequestMetadata);
      return (serverContext, metadataBytes.CurrentEnumerationItem);
    }
  }
}
