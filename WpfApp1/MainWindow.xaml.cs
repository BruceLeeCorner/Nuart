using NKit.Uart;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Text;
using System.Windows;
using TestNKitUart;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private int i = 0;

        private EbaraDryPump ebaraDryPump;


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
             ebaraDryPump = new EbaraDryPump("COM80", 9600, Parity.None, StopBits.One);
        }

        private void Device_DataRead(SerialBase.SerialEventArgs<byte[]> obj)
        {
        
        }

        private void Device_CompletedPackageReceived(SerialBase.SerialEventArgs<byte[]> e)
        {
         
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            
        }

        private void ButtonCom3_OnClick(object sender, RoutedEventArgs e)
        {
          
        }

        private void ButtonCom2_OnClick(object sender, RoutedEventArgs e)
        {
        
        }

        private void StartEbaraDryPump_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (i == 0)
                {
                    ++i;
                    while (true)
                    {
                        var r = ebaraDryPump.QueryStatus();
                    }
                }
            });
        }

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            ebaraDryPump.Reset();
        }
    }
}