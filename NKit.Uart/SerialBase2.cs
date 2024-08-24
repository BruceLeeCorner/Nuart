using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace NKit.Uart
{
    public abstract class SerialBase2
    {
        private readonly List<byte> _receiveBuffer;
        private readonly AutoResetEvent _resetPortEvent = new AutoResetEvent(false);
        private readonly Timer _timer;
        private readonly object _transitObject = new object();

        private bool _toResetFlag;

        private SerialPort _serialPort;

        protected SerialBase2(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            StopBits = stopBits;
            DataBits = dataBits;
            _receiveBuffer = new List<byte>();
            _timer = new Timer(CallBack);
            _timer.Change(TimeSpan.FromMilliseconds(30), Timeout.InfiniteTimeSpan);
        }

        public event EventHandler<SerialEventArgs> DataReceived;

        public event EventHandler<SerialEventArgs> DataSent;

        public int BaudRate { get; }

        public int DataBits { get; }

        public Parity Parity { get; }

        public string PortName { get; }

        public StopBits StopBits { get; }

        public void Reset()
        {
            lock (_transitObject)
            {
                _resetPortEvent.Reset();
                _toResetFlag = true;
                _resetPortEvent.WaitOne();
                // 等待计时器任务结束，然后暂停计时器
            }
        }

        protected abstract bool IsExpectedReply(byte[] requestBytes, byte[] bytesHasRead, int bytesLengthToRead, int checkTimes);

        protected abstract void IsExpectedReply(byte[] _buff, out int index);

        protected void Send(byte[] bytes)
        {
            lock (_transitObject)
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                _serialPort.Write(bytes, 0, bytes.Length);
            }
            DataSent?.Invoke(this, new SerialEventArgs(bytes));
        }

        private void CallBack(object state)
        {
            // 定时从接收缓存中取出数据。
            // 每取一次，若5ms内又收到一批新的数据，继续取。最多连续取10次。
            if (_serialPort.BytesToRead > 0)
            {
                var i = 0;// 防止一直有数据
                do
                {
                    var temp = new byte[_serialPort.BytesToRead];
                    // 注意Read不能死等！不要在其他线程轻易调用DiscardIn/OutBuff,会造成Read死等。
                    // Read阻塞型API. 直到缓冲区至少有1个字节可读便返回，如果长时间缓冲区为0，则一直阻塞，直到超时抛出TimeoutException,如果Timeout值是-1，则一直阻塞。
                    // temp.Length - offset 如果是0，表示不读取，任何情况下Read都会立刻返回，不存在阻塞情况。
                    int count = _serialPort.Read(temp, 0, temp.Length);
                    if (count > 0)
                    {
                        _receiveBuffer.AddRange(temp.Take(count));
                    }

                    if (false) // 这里检查一次完整帧，若完整且缓存区无数据就Break。
                    {
                        break;
                    }


                    // ReSharper disable once AccessToDisposedClosure
                } while (SpinWait.SpinUntil(() => _serialPort.BytesToRead > 0, 5) && i++ < 10);
            }

            IsExpectedReply(_receiveBuffer.ToArray(), out var index);

            if (index >= 0)
            {
                var frame = _receiveBuffer.Take(index).ToArray();
                _receiveBuffer.RemoveRange(0, index);
                DataReceived?.Invoke(this, new SerialEventArgs(frame));
            }

            if (_toResetFlag)
            {
                _toResetFlag = false;
                _receiveBuffer.Clear();
                _serialPort?.Dispose();
                _serialPort = null;
                _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                _resetPortEvent.Set();
            }

            _timer.Change(TimeSpan.FromMilliseconds(25), Timeout.InfiniteTimeSpan);
        }

        public class SerialEventArgs : EventArgs
        {
            public SerialEventArgs(byte[] data)
            {
                Data = data;
            }

            public byte[] Data { get; }
        }
    }
}