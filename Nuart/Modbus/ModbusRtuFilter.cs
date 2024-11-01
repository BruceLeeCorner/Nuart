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
            if(lastDataSent == null || dataReceived == null) return false;

            if (lastDataSent[1] == 0x01)
            {
                if (dataReceived.Length >= 3)
                {
                    if (dataReceived[2] + 3 + 2 == dataReceived.Length)
                    {
                        return true;
                    }
                }
            }
            else if (lastDataSent[1] == 0x02)
            {

            }
            return false;
        }
    }
}
