using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Nuart.Modbus.TestApp
{
    [DisplayName("Serial Settings")]
    public class SerialSettings : BindableBase
    {
        private int baudRate;
        private Parity parity;
        private string portName;

        private StopBits stopBits;

        [DisplayName("Port")]
        [ItemsSource(typeof(PortNameItemsSource))]
        [PropertyOrder(0)]
        public string PortName
        {
            get { return portName; }
            set { SetProperty(ref portName, value); }
        }

        [DisplayName("Baud Rate")]
        [ItemsSource(typeof(BaudRateItemsSource))]
        [PropertyOrder(1)]
        public int BaudRate
        {
            get { return baudRate; }
            set { SetProperty(ref baudRate, value); }
        }

        [DisplayName("Parity")]
        [PropertyOrder(2)]
        public Parity Parity
        {
            get { return parity; }
            set { SetProperty(ref parity, value); }
        }


        [DisplayName("Stop Bits")]
        [ItemsSource(typeof(StopBitsItemsSource))]
        [PropertyOrder(3)]
        public StopBits StopBits
        {
            get { return stopBits; }
            set { SetProperty(ref stopBits, value); }
        }
    }

    public class BaudRateItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            ItemCollection baudRates = new ItemCollection();
            baudRates.Add(4800);
            baudRates.Add(9600);
            baudRates.Add(19200);
            baudRates.Add(38400);
            baudRates.Add(115200);
            return baudRates;
        }
    }

    public class PortNameItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            ItemCollection portNames = new ItemCollection();

            foreach (var item in Enumerable.Range(1, 200))
            {
                portNames.Add("COM" + item);
            }
            return portNames;
        }
    }

    public class StopBitsItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            ItemCollection stopBits = new ItemCollection();
            stopBits.Add(StopBits.One, "1");
            stopBits.Add(StopBits.OnePointFive, "1.5");
            stopBits.Add(StopBits.Two, "2");
            return stopBits;
        }
    }
}