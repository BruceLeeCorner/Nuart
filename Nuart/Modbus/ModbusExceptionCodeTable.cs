using System.Linq;

namespace Nuart.Modbus
{
    public class ModbusExceptionCodeTable
    {
        public static ModbusExceptionCodeTable Instance { get; } = new ModbusExceptionCodeTable();

        private readonly (byte exceptionCode, string exceptionDescription)[] _innerTable;

        private ModbusExceptionCodeTable()
        {
            _innerTable = new (byte exceptionCode, string exceptionDescription)[]
            {
                (0x01, "IllegalFunction"),
                (0x02, "IllegalDataAddress"),
                (0x03, "IllegalDataValue"),
                (0x04, "SlaveDeviceFailure"),
                (0x05, "Acknowledge"),
                (0x06, "SlaveDeviceBusy"),
                (0x08, "MemoryParityError"),
                (0x0A, "GatewayPathUnavailable"),
                (0x0B, "GatewayTargetDeviceFailedToRespond"),
            };
        }

        public string GetDescription(byte exceptionCode) => _innerTable.Single(item => item.exceptionCode == exceptionCode).exceptionDescription;
    }
}