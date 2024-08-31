using System.IO.Ports;
using System.Windows;
using NLog;

namespace Nuart.DeviceSamples.Chillers.Src
{
    /// <summary>
    /// Interaction logic for NuanxinTD2000GChillerWindow.xaml
    /// </summary>
    public partial class NuanxinTD2000GChillerWindow : Window
    {
        private readonly NuanxinTD2000GChiller _device = new("COM85", 9600, Parity.None, StopBits.One);
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private int _i;
        private int _j;

        public NuanxinTD2000GChillerWindow()
        {
            InitializeComponent();
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