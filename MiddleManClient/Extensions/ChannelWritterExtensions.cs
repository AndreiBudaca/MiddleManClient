using System.Threading.Channels;

namespace MiddleManClient.Extensions
{
  public static class ChannelWritterExtensions
  {
    public static async Task WriteChunkedData(this ChannelWriter<byte[]> writer, int chunkSize, byte[] data)
    {
      int offset = 0;
      while (offset < data.Length)
      {
        int size = Math.Min(chunkSize, data.Length - offset);
        byte[] chunk = new byte[size];
        Array.Copy(data, offset, chunk, 0, size);
        await writer.WriteAsync(chunk);
        offset += size;
      }
    }
  }
}
