using System.IO.Ports;
using NLog;

namespace Nuart.DeviceSamples.Heaters.Src
{
    internal class OmronHeater : RequestReplyDeviceBase
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public OmronHeater(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
            Subscribe();
        }

        public OmronHeater(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Subscribe();
        }

        public Response<byte[]> QueryStatus()
        {
            return Request([0x01, 0x03, 0x20, 0x00, 0x00, 0x06, 0xce, 0x08]);
        }

        protected override bool FilterCompletedFrame(byte[] lastDataSent, byte[] dataReceivedBuffer, Func<bool> hasRemainingBytesInReadBuffer)
        {
            return dataReceivedBuffer.Length == 17 && !hasRemainingBytesInReadBuffer.Invoke();
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
                _logger.Debug("Read: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
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
    }
}