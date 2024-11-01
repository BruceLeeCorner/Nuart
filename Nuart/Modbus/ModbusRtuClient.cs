using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Nuart.RequestReplyModel;

namespace Nuart.Modbus
{
    public class ModbusRtuClient : IModbusClient
    {
        private SerialInterface<ModbusRtuFilter> serialInterface;

        public ModbusRtuClient()
        {
            serialInterface = new SerialInterface<ModbusRtuFilter> ("COM2");
        }

        public Response<bool[]> FC01(int slaveAddress, int startCoilAddress, int coilQuantity, int responseTimeout)
        {
            try
            {
                var reqBytes = Modbus.FC01.BuildRtuRequest(slaveAddress, startCoilAddress, coilQuantity);
                var response = serialInterface.Request(reqBytes, responseTimeout);
                if (response.IsSuccess)
                {
                    var lgth = response.Data.Length;
                    (int high, int low) = DataVerifier.Crc16Modbus(response.Data, 0, lgth - 2);
                    if (high != response.Data[lgth - 1] || low != response.Data[lgth - 2])
                    {
                        return new Response<bool[]>(null, "CRC16 check failed.");
                    }

                    bool normal = Modbus.FC01.ResolveRtuResponse(response.Data, out int slaveAddress2, out bool[] values, out byte exceptionCode);
                    if (normal == false)
                    {
                        return new Response<bool[]>(null, $"exception code: {exceptionCode}: exception description: {ModbusExceptionCodeTable.Instance.GetDescription(exceptionCode)}");
                    }
                    if (slaveAddress2 != slaveAddress)
                        return new Response<bool[]>(null, $"Slave address in response is {slaveAddress2},and it is not same as the corresponding request's.");
                    return new Response<bool[]>(values.Take(coilQuantity).ToArray(), response.Exception, response.ErrorMsg);
                }
                else
                {
                    return new Response<bool[]>(null, response.Exception, response.ErrorMsg);
                }
            }
            catch (Exception ex)
            {
                return new Response<bool[]>(null, ex);
            }
        }

        public Response<byte[]> FC03Byte(int slaveAddress, int startHoldingAddress, int quantity, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<double[]> FC03Double(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<float[]> FC03Float(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<short[]> FC03Int16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder2 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<int[]> FC03Int32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<long[]> FC03Int64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<ushort[]> FC03UInt16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder2 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<uint[]> FC03UInt32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }

        public Response<ulong[]> FC03UInt64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout)
        {
            throw new NotImplementedException();
        }
    }
}