using NKit.Uart;
using NLog;
using System.IO.Ports;

namespace WpfApp1
{
    public class TrumpPower : RequestReplyDeviceBase
    {
        public TrumpPower(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
            Subscribe();
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public TrumpPower(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Subscribe();
        }

        private void Subscribe()
        {
            Tag = this.GetType().Name;

            this.DataSent += args =>
            {
                _logger.Info("Fire: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            this.DataRead += args =>
            {
                _logger.Info("Read: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            this.CompletedPackageReceived += args =>
            {
                _logger.Info("Pack: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            this.TimedDataReadingJobThrowException += args =>
            {
                _logger.Error(args.Data, "Expt: {0} {1}", args.PortName, args.Tag);
            };
        }

        public Response<byte[]> QueryStatus()
        {
            return Request([0xaa, 0x02, 0x16, 0x00, 0x01,
                0x75, 0x00, 0x01,0xff,0x12,
                0x00,0x01,0xff,0x14,0x00,
                0x01, 0xff,0x07,0x02,0x01,
                0xff, 0x04,0x02,0x01, 0xff,0xbd,0x85,0x55]);
        }

        protected override bool FilterCompletedPackages(byte[] lastDataSent, byte[] dataReceivedBufferCopy, Func<bool> hasRemainingBytesInReadBuffer)
        {
            return dataReceivedBufferCopy.Length >= 54 && !hasRemainingBytesInReadBuffer.Invoke();
        }
    }
}