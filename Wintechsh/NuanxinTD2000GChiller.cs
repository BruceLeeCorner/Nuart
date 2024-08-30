using NKit.Uart;
using NLog;
using System.IO.Ports;

namespace TestNKitUart
{
    /// <summary>
    /// RS-485 2Wire
    /// </summary>
    internal class NuanxinTD2000GChiller : RequestReplyDeviceBase
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
            Tag = this.GetType().Name;

            this.DataSent += args =>
            {
                _logger.Info("Request: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            this.DataRead += args =>
            {
                _logger.Info("Read: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            this.CompletedPackageReceived += args =>
            {
                _logger.Info("Package: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            this.TimedDataReadingJobThrowException += args =>
            {
                _logger.Error(args.Data, "Exception: {0} {1}", args.PortName, args.Tag);
            };
        }

        protected override bool FilterCompletedPackages(byte[] lastDataSent, byte[] dataReceivedBufferCopy, Func<bool> hasRemainingBytesInReadBuffer)
        {
            return dataReceivedBufferCopy.Length > 0 &&
                   SpinWait.SpinUntil(hasRemainingBytesInReadBuffer, this.CalculateTransmissionTime(1)) == false;
        }
    }
}