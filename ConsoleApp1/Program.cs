// See https://aka.ms/new-console-template for more information

using System.IO.Ports;
using System.Threading.Channels;

Console.WriteLine($"{new d().dd}");

while (true)
{
    SerialPort sp = new SerialPort("COM256", 9600, Parity.Even, 8, StopBits.Two);
    var start = Environment.TickCount;
    sp.Open();
    var end = Environment.TickCount;
    Console.WriteLine(end - start);
    sp.Dispose();
    Thread.Sleep(25);
}


class d
{
    public string dd;
}