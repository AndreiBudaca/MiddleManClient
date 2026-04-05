using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Channels;

namespace MiddleManClient.Extensions
{
  public static class HubConnectionExtensions
  {
    public static async Task SendChunksAsync(this HubConnection connection, string serverMethod, int chunkSize, byte[] data)
    {
      var channel = Channel.CreateBounded<byte[]>(1);
      await connection.SendAsync(serverMethod, channel.Reader);
      await channel.Writer.WriteChunkedData(chunkSize, data);
      channel.Writer.Complete();
    }
  }
}
