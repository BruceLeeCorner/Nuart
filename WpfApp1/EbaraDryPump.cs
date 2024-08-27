using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using NKit.Uart;
using NLog;

namespace TestNKitUart
{
    internal class EbaraDryPump:SerialBase
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        
        public EbaraDryPump(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
           Sub();  
        }

        public EbaraDryPump(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Sub();
        }

        public Response<byte[]> QueryStatus()
        {
            return Request(new byte[] { 0x02, 0x4D, 0X32, 0X31, 0X03, 0X42, 0X35, 0X0D });
        }

        private void Sub()
        {
            Tag = "EbaraDryPump";

            this.DataSent += args =>
            {
                _logger.Info("Send: {0} {1} {2}", args.PortName,args.Tag,BitConverter.ToString(args.Data));
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
                _logger.Error(args.Data,"Expt: {0} {1}", args.PortName,args.Tag);
            };
        }


        protected override void FilterCompletedPackages(byte[] dataReceivedBufferCopy, [UnscopedRef] out int[] packageEndingIndexesInBufferCopy,
            Func<bool> hasRemainingBytesInReadBuffer)
        {
            packageEndingIndexesInBufferCopy = null;
            List<int> packageEndingIndexes = new List<int>();
            for (int i = 0; i < dataReceivedBufferCopy.Length; i++)
            {
                if (dataReceivedBufferCopy[i] == (byte)'\r')
                {
                    packageEndingIndexes.Add(i);
                }
            }

            packageEndingIndexesInBufferCopy = packageEndingIndexes.ToArray();
        }
    }
}
