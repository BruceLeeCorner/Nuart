using System;
using System.IO.Ports;
using System.Linq;

namespace NKit.Uart
{
    internal class SampleDevice2 : SerialBase
    {
        public SampleDevice2(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
        }

        public SampleDevice2(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
        }

        // 单条请求会响应2个帧
        // A[CR] ?[CR]
        protected override void FilterCompletedPackages(byte[] dataReceivedBufferCopy,
            out int[] packageEndingIndexesInBufferCopy, Func<bool> hasRemainingBytesInReadBuffer)
        {
            packageEndingIndexesInBufferCopy = null;
            if (dataReceivedBufferCopy.Contains((byte)'A'))
            {
                if (dataReceivedBufferCopy.Count(item => item == (byte)'\r') == 2)
                {
                    packageEndingIndexesInBufferCopy = new[] { 3 };
                }
            }
            else
            {
                if (dataReceivedBufferCopy.Contains((byte)'\r'))
                {
                    packageEndingIndexesInBufferCopy = new[] { 1 };
                }
            }
        }
    }
}