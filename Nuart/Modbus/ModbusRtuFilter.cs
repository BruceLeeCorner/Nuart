using Nuart.RequestReplyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuart.Modbus
{
    internal class ModbusRtuFilter : IReceiveFilter
    {
        public bool IsCompletedFrame(byte[] lastDataSent, byte[] dataReceived, Func<bool> hasBytesToRead)
        {
            
        }
    }
}
