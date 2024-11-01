using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuart.Modbus
{
    public static class ArgumentChecker
    {
        public static void CheckSlaveAddress01(int address)
        {
            CheckSlaveAddressRange(1, 247, address);
        }

        public static void CheckRegisterAddress01(int address)
        {
            CheckRegisterAddressRange(0x0001, 0xFFFF, address);
        }

        public static void CheckRegisterQuantity01(int address)
        {
            CheckRegisterQuantityRange(0x0001, 0x07d0, address);
        }

        private static void CheckRegisterAddressRange(int min, int max, int address)
        {
            if (address < min || address > max)
            {
                var ex = new ArgumentOutOfRangeException(nameof(address), $"valid range: [{min},{max}]");
                ex.Data.Add(nameof(address), address);
                throw ex;
            }
        }

        private static void CheckSlaveAddressRange(int min, int max, int address)
        {
            if (address < min || address > max)
            {
                var ex = new ArgumentOutOfRangeException(nameof(address), $"valid range: [{min},{max}]");
                ex.Data.Add(nameof(address), address);
                throw ex;
            }
        }

        private static void CheckRegisterQuantityRange(int min, int max, int quantity)
        {
            if (quantity < min || quantity > max)
            {
                var ex = new ArgumentOutOfRangeException(nameof(quantity), $"valid range: [{min},{max}]");
                ex.Data.Add(nameof(quantity), quantity);
                throw ex;
            }
        }
    }
}