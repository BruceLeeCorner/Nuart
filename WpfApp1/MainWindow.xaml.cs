using NKit.Uart;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Text;
using System.Windows;

namespace WpfApp1
{
    internal class Device : SerialBase
    {
        public Device(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {
        }

        public Device(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, rtsEnable, handshake)
        {
        }

        protected override void FilterCompletedPackages(byte[] dataReceivedBufferCopy, [UnscopedRef] out int[]? packageEndingIndexesInBufferCopy, Func<bool> hasRemainingBytesInReadBuffer
            )
        {
            packageEndingIndexesInBufferCopy = null;
            if (dataReceivedBufferCopy.Length > 0)
            {
                packageEndingIndexesInBufferCopy = new[] { dataReceivedBufferCopy.Length - 1 };
            }
        }

        public void Send(byte[] bytes)
        {
            Request(bytes, 2000);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device device = new Device("COM2", 110, Parity.None, StopBits.One);

        public MainWindow()
        {
            InitializeComponent();
        }

        private int i = 0;

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (i == 0)
            {
                i++;
                Task.Run(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(100);
                        device.Send(new byte[] { 64, 67, 55 });
                    }
                });
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            device.CompletedPackageReceived += Device_CompletedPackageReceived;
            //device.DataRead += Device_DataRead;
        }

        private void Device_DataRead(SerialBase.SerialEventArgs<byte[]> obj)
        {
            Console.WriteLine(Encoding.ASCII.GetString(obj.Data));
        }

        private void Device_CompletedPackageReceived(SerialBase.SerialEventArgs<byte[]> e)
        {
            Console.WriteLine( Encoding.ASCII.GetString(e.Data));
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            device.Reset();
        }

        private void ButtonCom3_OnClick(object sender, RoutedEventArgs e)
        {
            device.Reset("COM3");
        }

        private void ButtonCom2_OnClick(object sender, RoutedEventArgs e)
        {
            device.Reset("COM2");
        }
    }
}