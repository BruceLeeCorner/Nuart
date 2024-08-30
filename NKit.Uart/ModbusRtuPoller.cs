//using System;
//using System.CodeDom;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.IO;
//using System.IO.Ports;
//using System.Linq;
//using System.Text;

//namespace NKit.Uart
//{
//    public class ModbusRtuPoller : Serial, IModbusPoller
//    {
//        public ModbusRtuPoller(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits) : base(portName, baudRate, parity, stopBits, dataBits)
//        {
//        }

//        #region 0x01

//        public Response<bool[]> FuncCode01(int slaveAddress, int startCoilAddress, int coilQuantity, int responseTimeout = 200)
//        {
//            try
//            {
//                // 构建请求包
//                var requestBytes =
//                    Uart.FuncCode01.BuildRtuRequest(slaveAddress, startCoilAddress, coilQuantity);
//                // 发送请求
//                var response = Request(requestBytes, responseTimeout);
//                // 正常响应
//                if (response.IsSuccess)
//                {
//                    bool normal = Uart.FuncCode01.ResolveRtuResponse(response.Data, out int slaveAddress2, out bool[] values, out byte exceptionCode);
//                    if (slaveAddress2 != slaveAddress)
//                    {
//                        throw new MissingMatchException($"Slave address in response is {slaveAddress2},and it is not {slaveAddress}.");
//                    }
//                    if (normal) // 正常报文
//                    {
//                        return new Response<bool[]>(values.Take(coilQuantity).ToArray(), response.ErrorMsg);
//                    }
//                    // ReSharper disable once RedundantIfElseBlock
//                    else // 异常报文
//                    {
//                        // ReSharper disable once UseArrayEmptyMethod
//                        return new Response<bool[]>(new bool[0], $"exception code: {exceptionCode}: exception description: {ModbusExceptionCodeTable.Instance.GetDescription(exceptionCode)}");
//                    }
//                }
//                else // 异常响应
//                {
//                    var errMsg = response.ErrorMsg + $"And hexadecimal response is {BitConverter.ToString(response.Data)}";
//                    // ReSharper disable once UseArrayEmptyMethod
//                    var values = new bool[0];
//                    return new Response<bool[]>(values, response.Exception, errMsg);
//                }
//            }
//            catch (Exception ex)
//            {
//                // ReSharper disable once UseArrayEmptyMethod
//                return new Response<bool[]>(new bool[0], ex);
//            }
//        }

//        #endregion 0x01

//        #region 0x03

//        public Response<byte[]> FuncCode03Byte(int slaveAddress, int startHoldingAddress, int quantity,
//    int responseTimeout = 200)
//        {
//            try
//            {
//                if (slaveAddress < 0 || slaveAddress > 255)
//                {
//                    throw new ArgumentException($"{nameof(slaveAddress)} was out of range [0,255].", nameof(slaveAddress));
//                }

//                if (startHoldingAddress < 0 || startHoldingAddress > 65535)
//                {
//                    throw new ArgumentException($"{startHoldingAddress} was out of range [0,65535].", nameof(startHoldingAddress));
//                }

//                if (quantity < 1 || quantity > 125)
//                {
//                    throw new ArgumentException($"{nameof(quantity)} was out of range [1,125].", nameof(quantity));
//                }

//                // 构建请求包
//                var requestBytes =
//                    FuncCode03.BuildRtuRequest(slaveAddress, startHoldingAddress, quantity);
//                // 发送请求
//                var response = Request(requestBytes, responseTimeout);
//                // 正常响应
//                if (response.IsSuccess)
//                {
//                    bool normal = FuncCode03.ResolveRtuResponse(response.Data, out int slaveAddress2, out byte[] values, out byte exceptionCode);
//                    if (normal) // 正常报文
//                    {
//                        if (slaveAddress2 != slaveAddress)
//                            throw new MissingMatchException($"Slave address in response is {slaveAddress2},and it is not same as the corresponding request's.");
//                        return new Response<byte[]>(values, response.Exception, response.ErrorMsg);
//                    }
//                    // ReSharper disable once RedundantIfElseBlock
//                    else // 异常报文
//                    {
//                        // ReSharper disable once UseArrayEmptyMethod
//                        return new Response<byte[]>(new byte[0], $"exception code: {exceptionCode}: exception description: {ModbusExceptionCodeTable.Instance.GetDescription(exceptionCode)}");
//                    }
//                }
//                else // 异常响应
//                {
//                    var errMsg = response.ErrorMsg + $"And hexadecimal response is {BitConverter.ToString(response.Data)}";
//                    // ReSharper disable once UseArrayEmptyMethod
//                    var values = new byte[0];
//                    return new Response<byte[]>(values, response.Exception, errMsg);
//                }
//            }
//            catch (Exception ex)
//            {
//                // ReSharper disable once UseArrayEmptyMethod
//                return new Response<byte[]>(new byte[0], ex);
//            }
//        }

//        public Response<ushort[]> FuncCode03UInt16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            CheckByteOrder(byteOrder, ByteOrder.AB, ByteOrder.BA);
//            if (quantity % 2 != 0)
//            {
//            }
//            var response = FuncCode03Byte(slaveAddress, startHoldingAddress, quantity, responseTimeout);
//            if (response.IsSuccess)
//            {
//                ushort[] shorts = new ushort[response.Data.Length / 2];

//                for (int i = 0; i < response.Data.Length; i++)
//                {
//                    ushort value;
//                    if (byteOrder == ByteOrder.AB)
//                    {
//                        value = BitConverter.IsLittleEndian
//                           ? BitConverter.ToUInt16(new[] { response.Data[i + 1], response.Data[i] }, 0)
//                           : BitConverter.ToUInt16(new[] { response.Data[i], response.Data[i + 1] }, 0);
//                    }
//                    else
//                    {
//                        value = BitConverter.IsLittleEndian
//                           ? BitConverter.ToUInt16(new[] { response.Data[i], response.Data[i + 1] }, 0)
//                           : BitConverter.ToUInt16(new[] { response.Data[i + 1], response.Data[i] }, 0);
//                    }

//                    shorts[i / 2] = value;
//                }

//                return new Response<ushort[]>(shorts, response.Exception, response.ErrorMsg);
//            }

//            return new Response<ushort[]>(new ushort[0], response.Exception, response.ErrorMsg);
//        }

//        public Response<short[]> FuncCode03Int16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            var response = FuncCode03UInt16(slaveAddress, startHoldingAddress, quantity, byteOrder, responseTimeout);
//            return new Response<short[]>(response.Data.Select(item => (short)item).ToArray(), response.Exception,
//                response.ErrorMsg);
//        }

//        public Response<uint[]> FuncCode03UInt32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            CheckByteOrder(byteOrder, ByteOrder.ABCD, ByteOrder.BADC, ByteOrder.CDAB, ByteOrder.DCBA);
//            var response = FuncCode03Byte(slaveAddress, startHoldingAddress, quantity, responseTimeout);
//            if (response.IsSuccess)
//            {
//                uint[] uints = new uint[response.Data.Length / 4];

//                for (int i = 0; i < response.Data.Length; i += 4)
//                {
//                    if (byteOrder == ByteOrder.ABCD)
//                    {
//                        uints[i / 4] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i + 3], response.Data[i + 2], response.Data[i + 1],
//                                    response.Data[i]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i], response.Data[i + 1], response.Data[i + 2],
//                                    response.Data[i + 3]
//                                }, 0);
//                    }
//                    else if (byteOrder == ByteOrder.DCBA)
//                    {
//                        uints[i / 4] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i], response.Data[i + 1], response.Data[i + 2],
//                                    response.Data[i + 3]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i + 3], response.Data[i + 2], response.Data[i + 1],
//                                    response.Data[i]
//                                }, 0);
//                    }
//                    else if (byteOrder == ByteOrder.CDAB)
//                    {
//                        uints[i / 4] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i + 1], response.Data[i], response.Data[i + 3],
//                                    response.Data[i + 2]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i + 2], response.Data[i + 3], response.Data[i],
//                                    response.Data[i + 1]
//                                }, 0);
//                    }
//                    else if (byteOrder == ByteOrder.BADC)
//                    {
//                        uints[i / 4] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i + 2], response.Data[i + 3], response.Data[i],
//                                    response.Data[i + 1]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[4]
//                                {
//                                    response.Data[i + 1], response.Data[i], response.Data[i + 3],
//                                    response.Data[i + 2]
//                                }, 0);
//                    }
//                }

//                return new Response<uint[]>(uints, response.Exception, response.ErrorMsg);
//            }

//            return new Response<uint[]>(new uint[0], response.Exception, response.ErrorMsg);
//        }

//        public Response<int[]> FuncCode03Int32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            var response = FuncCode03UInt32(slaveAddress, startHoldingAddress, quantity, byteOrder);
//            if (response.IsSuccess)
//            {
//                return new Response<int[]>(response.Data.Select(item => (int)item).ToArray(), response.Exception,
//                    response.ErrorMsg);
//            }
//            return new Response<int[]>(new int[0], response.Exception,
//                response.ErrorMsg);
//        }

//        public Response<ulong[]> FuncCode03UInt64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            CheckByteOrder(byteOrder, ByteOrder.ABCDEFGH, ByteOrder.HGFEDCBA, ByteOrder.GHEFCDAB, ByteOrder.BADCFEHG);
//            var response = FuncCode03Byte(slaveAddress, startHoldingAddress, quantity, responseTimeout);
//            if (response.IsSuccess)
//            {
//                ulong[] ulongs = new ulong[response.Data.Length / 8];

//                for (int i = 0; i < response.Data.Length; i += 8)
//                {
//                    if (byteOrder == ByteOrder.ABCDEFGH)
//                    {
//                        ulongs[i / 8] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i + 7], response.Data[i + 6], response.Data[i + 5],response.Data[i + 4],response.Data[i + 3],response.Data[i + 2],response.Data[i + 1],
//                                    response.Data[i]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i], response.Data[i + 1], response.Data[i + 2],
//                                    response.Data[i + 3],response.Data[i + 4],response.Data[i + 5],response.Data[i + 6],response.Data[i + 7]
//                                }, 0);
//                    }
//                    else if (byteOrder == ByteOrder.HGFEDCBA)
//                    {
//                        ulongs[i / 8] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i], response.Data[i + 1], response.Data[i + 2],
//                                    response.Data[i + 3],response.Data[i + 4],response.Data[i + 5],response.Data[i + 6],response.Data[i + 7]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i + 7], response.Data[i + 6], response.Data[i + 5],response.Data[i + 4],response.Data[i + 3],response.Data[i + 2],response.Data[i + 1],
//                                    response.Data[i]
//                                }, 0);
//                    }
//                    else if (byteOrder == ByteOrder.GHEFCDAB)
//                    {
//                        ulongs[i / 8] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i + 1], response.Data[i], response.Data[i + 3],
//                                    response.Data[i + 2],response.Data[i + 5],response.Data[i + 4],response.Data[i + 7],response.Data[i + 6]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i + 6], response.Data[i + 7], response.Data[4],
//                                    response.Data[i + 5], response.Data[i + 2], response.Data[i + 3],  response.Data[i],response.Data[i + 1]
//                                }, 0);
//                    }
//                    else if (byteOrder == ByteOrder.BADCFEHG)
//                    {
//                        ulongs[i / 8] = BitConverter.IsLittleEndian
//                            ? BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i + 6], response.Data[i + 7], response.Data[i + 5],
//                                    response.Data[i + 4], response.Data[i + 3],response.Data[i + 2],response.Data[i + 1],response.Data[i]
//                                }, 0)
//                            : BitConverter.ToUInt32(
//                                new byte[8]
//                                {
//                                    response.Data[i + 1], response.Data[i], response.Data[i + 3],
//                                    response.Data[i + 2],response.Data[i + 5],response.Data[i + 4],response.Data[i + 7],response.Data[i + 6]
//                                }, 0);
//                    }
//                }

//                return new Response<ulong[]>(ulongs, response.Exception, response.ErrorMsg);
//            }
//            return new Response<ulong[]>(new ulong[0], response.Exception, response.ErrorMsg);
//        }

//        public Response<long[]> FuncCode03Int64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            var response = FuncCode03UInt64(slaveAddress, startHoldingAddress, quantity, byteOrder, responseTimeout);
//            if (response.IsSuccess)
//            {
//                return new Response<long[]>(response.Data.Select(item => (long)item).ToArray(), response.Exception,
//                    response.ErrorMsg);
//            }
//            return new Response<long[]>(new long[0], response.Exception,
//                response.ErrorMsg);
//        }

//        public Response<float[]> FuncCode03Float(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            var response = FuncCode03Int32(slaveAddress, startHoldingAddress, quantity, byteOrder, responseTimeout);
//            if (response.IsSuccess)
//            {
//                return new Response<float[]>(response.Data.Select(item => (float)item).ToArray(), response.Exception,
//                    response.ErrorMsg);
//            }
//            return new Response<float[]>(new float[0], response.Exception,
//                response.ErrorMsg);
//        }

//        public Response<double[]> FuncCode03Double(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder byteOrder,
//            int responseTimeout = 200)
//        {
//            CheckByteOrder(byteOrder, ByteOrder.ABCDEFGH, ByteOrder.HGFEDCBA, ByteOrder.GHEFCDAB, ByteOrder.BADCFEHG);

//            var response = FuncCode03UInt64(slaveAddress, startHoldingAddress, quantity, byteOrder, responseTimeout);
//            if (response.IsSuccess)
//            {
//                return new Response<double[]>(response.Data.Select(item => (double)item).ToArray(), response.Exception,
//                    response.ErrorMsg);
//            }
//            return new Response<double[]>(new double[0], response.Exception, response.ErrorMsg);
//        }

//        #endregion 0x03

//        protected override bool FilterCompletedFrame(byte[] requestBytes, byte[] bytesHasRead, int bytesLengthToRead, int checkTimes)
//        {
//            // 检查是不是异常响应报文
//            if (bytesHasRead.Length > 0)
//            {
//                if (bytesHasRead[1] >= 0x80)
//                {
//                    if (bytesHasRead.Length >= 5)
//                    {
//                        return true;
//                    }
//                }
//            }
//            // 功能码0x01
//            if (requestBytes[1] == 1)
//            {
//                var count = BitConverter.IsLittleEndian ? BitConverter.ToUInt16(new[] { requestBytes[5], requestBytes[4] }, 0) : BitConverter.ToUInt16(new[] { requestBytes[4], requestBytes[5] }, 0);

//                if (bytesHasRead.Length >= (int)Math.Ceiling(count * 1d / 8) + 5)
//                {
//                    return true;
//                }
//            }
//            // 功能码0x03
//            if (requestBytes[1] == 3)
//            {
//                var count = BitConverter.IsLittleEndian ? BitConverter.ToUInt16(new[] { requestBytes[5], requestBytes[4] }, 0) : BitConverter.ToUInt16(new[] { requestBytes[4], requestBytes[5] }, 0);
//                if (bytesHasRead.Length >= count * 2 + 5)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        protected override bool IsOnLine()
//        {
//            return true;
//        }

//        #region Check

//        private void CheckSlaveAddress(int slaveAddress)
//        {
//            if (slaveAddress < 0 || slaveAddress > 255)
//            {
//                throw new ArgumentException($"{slaveAddress} is out of range [0,255].", nameof(slaveAddress));
//            }
//        }

//        private void CheckRegisterAddress(int registerAddress)
//        {
//            if (registerAddress < 0 || registerAddress > 65535)
//            {
//                throw new ArgumentException($"{registerAddress} is out of range [0,65535].", nameof(registerAddress));
//            }
//        }

//        private void CheckQuantity(int quantityOfRegisterAddress)
//        {
//            if (quantityOfRegisterAddress < 0 || quantityOfRegisterAddress > 65535)
//            {
//                throw new ArgumentException($"{quantityOfRegisterAddress} is out of range [0,65535].", nameof(quantityOfRegisterAddress));
//            }
//        }

//        private void CheckByteOrder(ByteOrder byteOrder, params ByteOrder[] byteOrdersAccepted)
//        {
//            if (byteOrdersAccepted.All(item => item != byteOrder))
//            {
//                throw new ArgumentException($"ByteOrder must be one of {string.Join(new string(',', 1), byteOrdersAccepted)}.");
//            }
//        }

//        #endregion Check
//    }
//}