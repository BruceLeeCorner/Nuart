using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace NKit.Uart
{
    internal class SampleDevice1:SerialBase
    {
        public SampleDevice1(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits = 8, bool enableRts = false, Handshake handshake = Handshake.None) : base(portName, baudRate, parity, stopBits, dataBits, enableRts, handshake)
        {

        }

        protected override void FilterCompletedPackages(byte[] copyOfDataReceivedBuffer, Func<bool> hasBytesInReadBuffer, out int[] singlePackageEndingIndexes)
        {
            if (copyOfDataReceivedBuffer.Length > 0)
            {
                singlePackageEndingIndexes = SpinWait.SpinUntil(hasBytesInReadBuffer, 5) ? null : new[] { copyOfDataReceivedBuffer.Length - 1 };
                return;
            }
            singlePackageEndingIndexes = null;
        }
    }
}
