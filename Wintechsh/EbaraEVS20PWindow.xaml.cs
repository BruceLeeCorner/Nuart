using NLog;
using System.IO.Ports;
using System.Windows;
using TestNKitUart;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for EbaraEVS20PWindow.xaml
    /// </summary>
    public partial class EbaraEVS20PWindow : Window
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private EbaraEVS20PDryPump _device;
        private int _i;
        private int _j;

        public EbaraEVS20PWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _device = new EbaraEVS20PDryPump("COM80", 9600, Parity.None, StopBits.One);
        }

        private void ResetAlways_OnClick(object sender, RoutedEventArgs e)
        {
            if (_j == 0)
            {
                _j++;
                Task.Run(() =>
                {
                    while (true)
                    {
                        _device.Reset();
                    }
                });
            }
        }

        private void ResetOnce_OnClick(object sender, RoutedEventArgs e)
        {
            _device.Reset();
        }

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (_i == 0)
                {
                    ++_i;
                    while (true)
                    {
                        var r = _device.QueryStatus();
                        if (r.IsSuccess == false || r.Data.Length != 37)
                        {
                            _logger.Error($"{r.IsSuccess}   {r.ErrorMsg}   {r.Data.Length}  {r.Exception}");
                        }
                    }
                }
            });
        }
    }
}