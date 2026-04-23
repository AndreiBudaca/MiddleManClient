using System.Threading.Channels;
using MiddleManClient.Buffer;
using MiddleManClient.Extensions;

namespace MiddleManClient.MethodProcessing.MethodFunctionHandlerGenerator.MethodResponseHandling.ResponseHandler
{
  public class ResponseWritingHandler(ChannelWriter<byte[]?> channelWriter, int maxChannelSize, IContentBuffer? requestContent = null)
  {
    private readonly ChannelWriter<byte[]?> _channelWriter = channelWriter;
    private readonly int _maxChannelSize = maxChannelSize;
    private readonly IContentBuffer? _requestContent = requestContent;
    private bool _isFirstWrite = true;

    public async Task Write(byte[] data, CancellationToken cancellationToken = default)
    {
      if (_isFirstWrite)
      {
        _isFirstWrite = false;
        if (_requestContent != null)
        {
          await _requestContent.BufferAllData(cancellationToken);
        }
      }

      await _channelWriter.WriteChunkedData(_maxChannelSize, data, cancellationToken);
    }
  }
}