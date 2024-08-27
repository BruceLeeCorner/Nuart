using System;
using System.IO.Ports;
using System.Threading;

namespace NKit.Uart
{
    /*
     * 协议帧无拆包特殊字节片段，上一帧与下一帧通过时间间隔确定。如ModbusRTU，超出3.5个字节时间未收到数据，则认为一帧接收完毕。
     */

    internal class SampleDevice1 : SerialBase
    {
        public SampleDevice1(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits = 8, bool rtsEnable = false, Handshake handshake = Handshake.None) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
        }

        protected override void FilterCompletedPackages(byte[] dataReceivedBufferCopy, out int[] packageEndingIndexesInBufferCopy, Func<bool> hasRemainingBytesInReadBuffer)
        {
            // 如果5个字节时间内没收到任何新的数据，则表示这是一个完整的帧，否则认为不是完整的一帧。
            // 适用于无标志字节的协议，如ModbusRTU以3.5个字符时间间隔来确认一个完整帧。
            packageEndingIndexesInBufferCopy = null;
            if (dataReceivedBufferCopy.Length > 0)
            {
                packageEndingIndexesInBufferCopy = SpinWait.SpinUntil(hasRemainingBytesInReadBuffer, CalculateTransmissionTime(1)) ? null : new[] { dataReceivedBufferCopy.Length - 1 };
            }
        }
    }
}