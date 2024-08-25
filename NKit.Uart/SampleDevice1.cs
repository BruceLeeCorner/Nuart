using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace NKit.Uart
{
    internal class SampleDevice1:SerialBase
    {
        public SampleDevice1(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits = 8, bool rtsEnable = false, Handshake handshake = Handshake.None) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {

        }

        protected override void FilterCompletedPackages(byte[] copyOfDataReceivedBuffer, Func<bool> hasBytesInReadBuffer, out int[] singlePackageEndingIndexes)
        {
            singlePackageEndingIndexes = null;
            if (copyOfDataReceivedBuffer.Length > 0)
            {
                // 如果5个字节时间内没收到任何新的数据，则表示这是一个完整的帧，否则认为不是完整的一帧。
                // 适用于无标志字节的协议，如ModbusRTU以3.5个字符时间间隔来确认一个完整帧
                singlePackageEndingIndexes = SpinWait.SpinUntil(hasBytesInReadBuffer, OneByteTransmissionTime * 5) ? null : new[] { copyOfDataReceivedBuffer.Length - 1 };
            }
        }
    }
}
