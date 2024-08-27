using NLog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TestNKitUart;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private int i = 0;
        private OmronHeater heater = new OmronHeater("COM40", 9600, Parity.Even, StopBits.Two);
        private Logger _logger = LogManager.GetLogger("OmronHeater");
        public Window1()
        {
            InitializeComponent();
        }

        private void StartHeater_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (i == 0)
                {
                    ++i;
                    while (true)
                    {
                        var r = heater.QueryStatus();
                        if (r.IsSuccess == false || r.Data.Length != 8)
                        {
                            _logger.Error($"{r.IsSuccess}   {r.ErrorMsg}   {r.Data.Length}");
                        }
                    }
                }
            });
        }
    }
}