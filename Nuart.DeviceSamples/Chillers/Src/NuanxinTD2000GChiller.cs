using System.IO.Ports;
using NKit.Uart;
using NLog;

namespace Nuart.DeviceSamples.Chillers.Src
{
    /// <summary>
    /// RS-485 2Wire
    /// </summary>
    internal class NuanxinTD2000GChiller : RequestReplySerialBase
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public NuanxinTD2000GChiller(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
            Subscribe();
        }

        public NuanxinTD2000GChiller(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Subscribe();
        }

        public Response<byte[]> QueryStatus()
        {
            return Request([0x01, 0x03, 0x00, 0x00, 0x00, 0x10, 0x44, 0x06]);
        }

        private void Subscribe()
        {
            Tag = GetType().Name;

            DataSent += args =>
            {
                _logger.Info("Request: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            DataRead += args =>
            {
                _logger.Info("Read: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            CompletedFrameReceived += args =>
            {
                _logger.Info("Reply: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            TimedDataReadingJobThrowException += args =>
            {
                _logger.Error(args.Data, "Exception: {0} {1}", args.PortName, args.Tag);
            };
        }

        protected override bool FilterCompletedFrame(byte[] lastDataSent, byte[] dataReceivedBuffer, Func<bool> hasRemainingBytesInReadBuffer)
        {
            return dataReceivedBuffer.Length > 0 &&
                   SpinWait.SpinUntil(hasRemainingBytesInReadBuffer, CalculateTransmissionTime(1)) == false;
            // 问题点： Read Reply显示的接收的响应字节数量和字节内容都是正常的，但是Request函数却报错超时，报错信息显示的字节数量是37却是正常的。
            // 原因是：只等待了1个字节时间，太短了，导致以为一个帧结束，Request被释放后，然后

            // Request: COM85 NuanxinTD2000GChiller 01-03-00-00-00-10-44-06 |
            // Read: COM85 NuanxinTD2000GChiller 01-03-20-00-00-00-C8-01-F4-01-83-01-83-00-00-00-EB-00-03-00-00-00-00-01-2C-01-F4-01-E8-01-E8-00-00-00-EB-C2-4D |
            // Reply: COM85 NuanxinTD2000GChiller 01-03-20-00-00-00-C8-01-F4-01-83-01-83-00-00-00-EB-00-03-00-00-00-00-01-2C-01-F4-01-E8-01-E8-00-00-00-EB-C2-4D |
            // ********************************************************************
            //2024-08-30 18:02:07.0960 | ERR | Wintechsh.NuanxinTD2000GChillerWindow | False   Response timeout. Maybe no data was received or received data can't be resolved a completed Frame.   37   |
        }
    }
}