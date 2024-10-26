using Nuart.RequestReplyModel;

namespace Nuart.DeviceSamples.Chillers.Src
{
    internal class Filter : IReceiveFilter
    {
        public bool IsCompletedFrame(byte[] lastDataSent, byte[] dataReceived, bool hasBytesToRead)
        {
            return dataReceived.Length > 0 &&
                  SpinWait.SpinUntil(() => hasBytesToRead, 100) == false;
        }
    }
}