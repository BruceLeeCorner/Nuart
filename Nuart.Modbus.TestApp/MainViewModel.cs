using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuart.Modbus.TestApp
{
    public class MainViewModel
    {
        [SetsRequiredMembers]
        public MainViewModel()
        {
            //SerialSettings = new SerialSettings();
            //SerialSettings.PortName = "COM1";
            //SerialSettings.BaudRate = 9600;
            //SerialSettings.StopBits = System.IO.Ports.StopBits.One;
            //SerialSettings.Parity = System.IO.Ports.Parity.None;

            AssignCommands();
        }

        private void AssignCommands()
        {
            ResetCommand = new DelegateCommand(Reset);
            ReadCommand = new DelegateCommand<string>(Read);
        }

        private void Read(string obj)
        {
            int code = System.Convert.ToInt32(obj);
            if (code == 1)
            {
            }
            else if (code == 2) { }
            else if (code == 3) { }
            else if (code == 4) { }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Reset()
        {
        }

        public S SerialSettings { get; set; } = new S();

        public DelegateCommand ResetCommand { get; private set; } = null!;
        public DelegateCommand<string> ReadCommand { get; private set; } = null!;
    }

   public class S
    {
        public List<int> SerialSettings { get; set; } = new List<int>() { 1, 2, 3 };
    }
}