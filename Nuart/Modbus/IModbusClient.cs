namespace Nuart.Modbus
{
    public interface IModbusClient
    {
        Response<bool[]> FC01(int slaveAddress, int startCoilAddress, int coilQuantity, int responseTimeout);

        #region FuncCode03
        Response<byte[]> FC03Byte(int slaveAddress, int startHoldingAddress, int quantity, int responseTimeout);

        Response<ushort[]> FC03UInt16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder2 byteOrder, int responseTimeout);

        Response<short[]> FC03Int16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder2 byteOrder, int responseTimeout);

        Response<uint[]> FC03UInt32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout);

        Response<int[]> FC03Int32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout);

        Response<ulong[]> FC03UInt64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout);

        Response<long[]> FC03Int64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout);

        Response<float[]> FC03Float(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout);

        Response<double[]> FC03Double(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout); 
        #endregion
    }
}