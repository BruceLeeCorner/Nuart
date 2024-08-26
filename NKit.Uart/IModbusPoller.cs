namespace NKit.Uart
{
    public interface IModbusPoller
    {
        Response<bool[]> FuncCode01(int slaveAddress, int startCoilAddress, int coilQuantity, int responseTimeout);

        Response<byte[]> FuncCode03Byte(int slaveAddress, int startHoldingAddress, int quantity, int responseTimeout);

        Response<ushort[]> FuncCode03UInt16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder2 byteOrder, int responseTimeout);

        Response<short[]> FuncCode03Int16(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder2 byteOrder, int responseTimeout);

        Response<uint[]> FuncCode03UInt32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout);

        Response<int[]> FuncCode03Int32(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout);

        Response<ulong[]> FuncCode03UInt64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout);

        Response<long[]> FuncCode03Int64(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout);

        Response<float[]> FuncCode03Float(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder4 byteOrder, int responseTimeout);

        Response<double[]> FuncCode03Double(int slaveAddress, int startHoldingAddress, int quantity, ByteOrder8 byteOrder, int responseTimeout);
    }
}