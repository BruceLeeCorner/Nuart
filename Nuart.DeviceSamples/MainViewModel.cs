using System.IO.Ports;

namespace Nuart.DeviceSamples
{
    public class MainViewModel : BindableBase
    {
        public IEnumerable<string> PortNames { get; }
        public int[] BaudRateOptions { get; }
        public string[] StopBitsOptions { get; }
        public int[] DataBitsOptions { get; }
        public string[] ParityOptions { get; }

        public RequestReplySerialInterface SerialBase { get; set; }

        public MainViewModel()
        {
            PortNames = Enumerable.Range(1, 200).Select(item => "COM" + item);
            BaudRateOptions =
            [
                2400,4800,9600,19200,38400,57600,115200
            ];
            DataBitsOptions = [5, 6, 7, 8, 9];
            StopBitsOptions = [StopBits.One.ToString(), StopBits.OnePointFive.ToString(), StopBits.Two.ToString()];
            ParityOptions = Enum.GetNames<StopBits>();
        }
    }
}