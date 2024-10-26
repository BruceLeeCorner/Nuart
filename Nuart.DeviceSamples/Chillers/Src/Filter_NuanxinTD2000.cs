using Nuart.RequestReplyModel;

namespace Nuart.DeviceSamples.Chillers.Src
{
    internal class Filter_NuanxinTD2000 : IReceiveFilter
    {
        public bool IsCompletedFrame(byte[] lastDataSent, byte[] dataReceived, Func<bool> hasBytesToRead)
        {
            if (dataReceived.Length != 0)
            {
                Thread.Sleep(120);
                if (hasBytesToRead.Invoke() == false)
                {
                    return true;
                }
            }

            return false;
        }
    }
}