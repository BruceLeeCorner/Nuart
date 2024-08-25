using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace NKit.Uart
{
    internal class SampleDevice2:SerialBase
    {
        public SampleDevice2(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
        }

        public SampleDevice2(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
        }

        // 单条请求会响应2个帧
        // A[CR] ?[CR]
        protected override void FilterCompletedPackages(byte[] copyOfDataReceivedBuffer, Func<bool> hasBytesInReadBuffer,
            out int[] singlePackageEndingIndexes)
        {
            singlePackageEndingIndexes = null;
            if (copyOfDataReceivedBuffer.Contains((byte)'A'))
            {
                if (copyOfDataReceivedBuffer.Count(item => item == (byte)'\r') == 2)
                {
                    singlePackageEndingIndexes = new[] { 3 };
                }
            }
            else
            {
                if (copyOfDataReceivedBuffer.Contains((byte)'\r'))
                {
                    singlePackageEndingIndexes = new[] { 1 };
                }
            }
        }
    }
}
