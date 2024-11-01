using System.ComponentModel;

namespace Nuart.Modbus.TestApp
{
    public class ReadParameter : BindableBase
    {
        private int quantity;
        private int slaveAddr;
        private int startAddr;

        [DisplayName("Quantity")]
        public int Quantity
        {
            get { return quantity; }
            set { SetProperty(ref quantity, value); }
        }

        [DisplayName("Slave")]
        public int SlaveAddr
        {
            get { return slaveAddr; }
            set { SetProperty(ref slaveAddr, value); }
        }

        [DisplayName("Start Address")]
        public int StartAddr
        {
            get { return startAddr; }
            set { SetProperty(ref startAddr, value); }
        }
    }
}