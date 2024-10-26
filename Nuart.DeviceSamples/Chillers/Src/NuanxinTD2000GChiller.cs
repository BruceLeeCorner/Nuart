using NLog;
using Nuart.RequestReplyModel;
using System.IO.Ports;

namespace Nuart.DeviceSamples.Chillers.Src
{
    /// <summary>
    /// RS-485 2Wire
    /// </summary>
    internal class NuanxinTD2000GChiller
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private SerialInterface<Filter_NuanxinTD2000> serialInterface;

        public NuanxinTD2000GChiller(string portName, int baudRate, Parity parity, StopBits stopBits)
        {
            serialInterface = new SerialInterface<Filter_NuanxinTD2000>(portName, baudRate, parity, stopBits);
            serialInterface.Tag = GetType().Name;
            Subscribe();
        }

        public Response<byte[]> QueryStatus()
        {
            return serialInterface.Request([0x01, 0x03, 0x00, 0x00, 0x00, 0x10, 0x44, 0x06]);
        }

        private void Subscribe()
        {
            serialInterface.DataSent += args =>
            {
                _logger.Info("Request: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            serialInterface.DataRead += args =>
            {
                _logger.Info("Read: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            serialInterface.CompletedFrameReceived += args =>
            {
                _logger.Info("Reply: {0} {1} {2}", args.PortName, args.Tag, BitConverter.ToString(args.Data));
            };

            serialInterface.TimedDataReadingJobThrowException += args =>
            {
                _logger.Error(args.Data, "Exception: {0} {1}", args.PortName, args.Tag);
            };
        }
    }
}