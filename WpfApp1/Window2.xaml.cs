using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        private Chiller chiller = new Chiller("COM85",9600,Parity.None,StopBits.One);
        private int i = 0;
        private Logger _logger = LogManager.GetLogger("NuanxinChiller");
        public Window2()
        {
            InitializeComponent();
        }

        private void Chiller_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (i == 0)
                {
                    ++i;
                    while (true)
                    {
                       var r = chiller.QueryStatus();
                       if (r.IsSuccess == false || r.Data.Length != 37)
                       {
                           _logger.Error($"{r.IsSuccess}   {r.ErrorMsg}   {r.Data.Length}" );
                       }
                    }
                }
            });
        }
    }
}
