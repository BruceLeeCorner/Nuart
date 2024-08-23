using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NKit.Uart
{
    public class FuncCode03
    {
        public static byte[] BuildRtuRequest(int slaveAddress, int startHoldingAddress, int quantity)
        {
            var bytes = new byte[8];
            bytes[0] = (byte)slaveAddress;
            bytes[1] = 3;
            byte high, low;
            var binaryRepresentation = BitConverter.GetBytes((ushort)startHoldingAddress);
            if (BitConverter.IsLittleEndian)
            {
                low = binaryRepresentation[0];
                high = binaryRepresentation[1];
            }
            else
            {
                low = binaryRepresentation[1];
                high = binaryRepresentation[0];
            }
            bytes[2] = high;
            bytes[3] = low;

            binaryRepresentation = BitConverter.GetBytes((ushort)quantity);
            if (BitConverter.IsLittleEndian)
            {
                low = binaryRepresentation[0];
                high = binaryRepresentation[1];
            }
            else
            {
                low = binaryRepresentation[1];
                high = binaryRepresentation[0];
            }
            bytes[4] = high;
            bytes[5] = low;

            (high, low) = DataVerifier.Crc16Modbus(bytes, 0, 6);
            bytes[6] = low;
            bytes[7] = high;
            return bytes;
        }

        public static bool ResolveRtuResponse(byte[] response, out int slaveAddress, out byte[] values, out byte exceptionCode)
        {
            // CRC校验
            var length = response.Length;
            var high = response[length - 1];
            var low = response[length - 2];
            var (high2, low2) = DataVerifier.Crc16Modbus(response, 0, length - 2);
            if (high != high2 || low != low2)
            {
                throw new InvalidDataException("CRC failed.");
            }
            // 解析Slave地址
            slaveAddress = response[0];
            // 解析功能码
            int function = response[1];
            if (function == 0x83)
            {
                exceptionCode = response[2];
                // ReSharper disable once UseArrayEmptyMethod
                values = new byte[0];
                return false;
            }
            if (function != 0x03)
            {
                throw new MissingMatchException($"Function code in Response is {function}, and it isn't 0x01.");
            }
            // 解析数据
            var bytesCount = response[2];

            values = new byte[bytesCount];
            Array.Copy(response, 3, values, 0, bytesCount);
            exceptionCode = 0x00;
            return true;
        }
    }
}