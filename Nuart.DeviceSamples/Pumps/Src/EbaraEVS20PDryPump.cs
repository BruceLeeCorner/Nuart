﻿using NLog;
using System.IO.Ports;

namespace Nuart.DeviceSamples.Pumps.Src
{
    //  RS232
    internal class EbaraEVS20PDryPump : RequestReplySerialInterface
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public EbaraEVS20PDryPump(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
            Subscribe();
        }

        public EbaraEVS20PDryPump(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
            Subscribe();
        }

        public Response<byte[]> QueryStatus()
        {
            return Request([0x02, 0x4D, 0X32, 0X31, 0X03, 0X42, 0X35, 0X0D], 150);
        }

        protected override bool FilterCompletedFrame(byte[] lastDataSent, byte[] dataReceivedBuffer, Func<bool> hasRemainingBytesInReadBuffer)
        {
            return dataReceivedBuffer.Length > 0 && dataReceivedBuffer[^1] == (byte)'\r';
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