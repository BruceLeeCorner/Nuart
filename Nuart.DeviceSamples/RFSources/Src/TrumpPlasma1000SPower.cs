using System.IO.Ports;
using NLog;

namespace Nuart.DeviceSamples.RFSources.Src
{
    // RS232
    public class TrumpPlasma1000SPower : RequestReplySerialBase
    {
        public TrumpPlasma1000SPower(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
            Subscribe();
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public TrumpPlasma1000SPower(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Subscribe();
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

        public Response<byte[]> QueryStatus()
        {
            return Request(
            [0xaa, 0x02, 0x16, 0x00, 0x01,
                0x75, 0x00, 0x01,0xff,0x12,
                0x00,0x01,0xff,0x14,0x00,
                0x01, 0xff,0x07,0x02,0x01,
                0xff, 0x04,0x02,0x01, 0xff,
                0xbd,0x85,0x55]);
        }

        protected override bool FilterCompletedFrame(byte[] lastDataSent, byte[] dataReceivedBuffer, Func<bool> hasRemainingBytesInReadBuffer)
        {
            return dataReceivedBuffer.Length >= 54 && !hasRemainingBytesInReadBuffer.Invoke();
        }
    }
}