using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NKit.Uart;

namespace WpfApp1
{

    class Device:SerialBase
    {
        public Device(string portName, int baudRate, Parity parity, StopBits stopBits) : base(portName, baudRate, parity, stopBits)
        {

        }

        public Device(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool enableRts, Handshake handshake) : base(portName, baudRate, parity, stopBits, dataBits, enableRts, handshake)
        {

        }

        protected override void FilterCompletedPackages(byte[] copyOfDataReceivedBuffer, Func<bool> hasBytesInReadBuffer,
            [UnscopedRef] out int[]? singlePackageEndingIndexes)
        {
            if (copyOfDataReceivedBuffer.Length > 0)
            {
                singlePackageEndingIndexes = SpinWait.SpinUntil(hasBytesInReadBuffer, 5) ? null : new[] { copyOfDataReceivedBuffer.Length - 1 };
                return;
            }
            singlePackageEndingIndexes = null;
        }


        public void Send(byte[] bytes)
        {
            Send(bytes,200);
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Device device = new Device("COM2", 9600, Parity.None, StopBits.One);
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    device.Send(new byte[] { 41, 42, 42 });
                }
            });
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            device.CompletedPackageReceived += Device_CompletedPackageReceived;
        }

        private void Device_CompletedPackageReceived(object? sender, SerialBase.SerialEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "   " + Encoding.ASCII.GetString(e.Data));
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            device.Reset();
        }

        private void ButtonCom3_OnClick(object sender, RoutedEventArgs e)
        {
            device.PortName = "COM3";
        }

        private void ButtonCom2_OnClick(object sender, RoutedEventArgs e)
        {
            device.PortName = "COM2";
        }
    }
}