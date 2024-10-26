namespace Nuart.RequestReplyModel
{
    public interface IReceiveFilter
    {
        bool IsCompletedFrame(byte[] lastDataSent, byte[] dataReceived, bool hasBytesToRead);
    }
}