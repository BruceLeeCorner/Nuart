using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace Nuart
{
    public class SerialEventArgs<T> : EventArgs
    {
        public SerialEventArgs(T data, object tag, string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, bool rtsEnable, Handshake handshake)
        {
            Data = data;
            Tag = tag;
            PortName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            StopBits = stopBits;
            Parity = parity;
            RtsEnable = rtsEnable;
            Handshake = handshake;
        }

        public int BaudRate { get; }
        public T Data { get; }
        public int DataBits { get; }
        public Handshake Handshake { get; }
        public Parity Parity { get; }
        public string PortName { get; }
        public bool RtsEnable { get; }
        public StopBits StopBits { get; }
        public object Tag { get; }
    }
}
