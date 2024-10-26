using System;

namespace Nuart.RequestReplyModel
{
    public interface IReceiveFilter
    {
        bool IsCompletedFrame(byte[] lastDataSent, byte[] dataReceived, Func<bool> hasBytesToRead);
    }
}