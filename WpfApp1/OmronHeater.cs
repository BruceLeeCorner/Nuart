using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NKit.Uart;
using NLog;

namespace TestNKitUart
{
    internal class OmronHeater : SerialBase
    {
        private Logger _logger = LogManager.GetLogger("OmronHeater");

        public OmronHeater(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
            Sub();
        }

        public OmronHeater(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Sub();
        }

        public Response<byte[]> QueryStatus()
        {
           return Request(new byte[] { 0x01, 0x03, 0x00, 0xc1, 0x00, 0x01, 0xd5, 0xf6 });
        }

        private void Sub()
        {
            Tag = "OmronHeater";

            this.DataSent += args =>
            {
                _logger.Info("Send: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
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

        protected override void FilterCompletedPackages(byte[] dataReceivedBufferCopy, [UnscopedRef] out int[] packageEndingIndexesInBufferCopy,
            Func<bool> hasRemainingBytesInReadBuffer)
        {
            packageEndingIndexesInBufferCopy = null;
            if (dataReceivedBufferCopy.Length > 0)
            {
                if (SpinWait.SpinUntil(hasRemainingBytesInReadBuffer, this.CalculateTransmissionTime(3)) == false)
                {
                    packageEndingIndexesInBufferCopy = new[] { dataReceivedBufferCopy.Length - 1 };
                }
            }
        }
    }
}