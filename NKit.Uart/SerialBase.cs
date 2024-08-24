using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace NKit.Uart
{
    public abstract class SerialBase : IDisposable
    {
        #region Fields

        private readonly List<byte> _dataReceivedBuffer;
        private readonly AutoResetEvent _replayEvent;
        private readonly AutoResetEvent _resetPortEvent;
        private readonly Timer _timer;
        private readonly object _transmissionLocker;
        private bool _resetFlag = false;
        private SerialPort _serialPort;

        #endregion Fields

        protected SerialBase(string portName, int baudRate, Parity parity, StopBits stopBits) : this(portName, baudRate, parity, stopBits, 8, false, Handshake.None)
        {
        }

        protected SerialBase(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool enableRts, Handshake handshake)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            StopBits = stopBits;
            Handshake = handshake;
            DataBits = dataBits;
            EnableRts = enableRts;
            _dataReceivedBuffer = new List<byte>();
            _replayEvent = new AutoResetEvent(false);
            _resetPortEvent = new AutoResetEvent(false);
            _transmissionLocker = new object();
            _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
            _serialPort.RtsEnable = EnableRts;
            _serialPort.Handshake = Handshake;
            _timer = new Timer(CallBack);
            _timer.Change(0, Timeout.Infinite);
        }

        public event EventHandler<SerialEventArgs> CompletedPackageReceived;

        /// <summary>
        /// 用于调试串口，强烈建议注册程序只是打印日志
        /// </summary>
        public event EventHandler<SerialEventArgs> DataReadFromInBuffer;

        public event EventHandler<SerialEventArgs> DataSent;

        #region Communication Options

        public int BaudRate { get; set; }

        public int DataBits { get; set; }

        /// <summary>
        /// True表示设备可以接收数据。这个信号只是决定输出信号给对方，允许对方随时可以向己方发送数据，对方用不用不管。
        /// </summary>
        public bool EnableRts { get; set; }

        /// <summary>
        /// 表示己方在发送数据前是否检查对方此刻允不允许发送数据。
        /// </summary>
        public Handshake Handshake { get; set; }

        public Parity Parity { get; set; }

        public string PortName { get; set; }

        public StopBits StopBits { get; set; }

        #endregion Communication Options

        public void Reset()
        {
            lock (_transmissionLocker)
            {
                _resetPortEvent.Reset();
                _resetFlag = true;
                _resetPortEvent.WaitOne();
            }
        }

        protected abstract void FilterCompletedPackages(byte[] copyOfDataReceivedBuffer, Func<bool> hasBytesInReadBuffer, out int[] singlePackageEndingIndexes);

        protected Response<byte[]> Request(byte[] bytes, int replyTimeout = 200)
        {
            if (replyTimeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(replyTimeout), "The argument should be greater than 0");
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            lock (_transmissionLocker)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                    }
                    _replayEvent.Reset();
                    _serialPort.ReadTimeout = replyTimeout;
                    var start = Environment.TickCount;
                    _serialPort.Write(bytes, 0, bytes.Length);
                    DataSent?.Invoke(this, new SerialEventArgs(bytes));
                    var end = Environment.TickCount;
                    var timeout = !_replayEvent.WaitOne(replyTimeout - (end - start));
                    return timeout ? new Response<byte[]>(_dataReceivedBuffer.ToArray(), "Response timeout.") : new Response<byte[]>(_dataReceivedBuffer.ToArray());
                }
                catch (Exception exception)
                {
                    return new Response<byte[]>(_dataReceivedBuffer.ToArray(), exception);
                }
            }
        }

        protected Response Send(byte[] bytes, int writeTimeout = 200)
        {
            lock (_transmissionLocker)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                    {
                        Thread.Sleep(writeTimeout);
                        if (!_serialPort.IsOpen)
                        {
                            throw new InvalidOperationException("Port hasn't opened.");
                        }
                    }

                    _serialPort.WriteTimeout = writeTimeout;
                    _serialPort.Write(bytes, 0, bytes.Length);
                    DataSent?.Invoke(this, new SerialEventArgs(bytes));
                    return new Response();
                }
                catch (Exception exception)
                {
                    return new Response(exception);
                }
            }
        }

        private void CallBack(object state)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                // 定时从接收缓存中取出数据。
                do
                {
                    var temp = new byte[_serialPort.BytesToRead];
                    // 注意Read不能死等！不要在其他线程轻易调用DiscardIn/OutBuff,会造成Read死等。
                    // Read阻塞型API. 直到缓冲区至少有1个字节可读便返回，如果长时间缓冲区为0，则一直阻塞，直到超时抛出TimeoutException,如果Timeout值是-1，则一直阻塞。
                    // temp.Length - offset 如果是0，表示不读取，任何情况下Read都会立刻返回，不存在阻塞情况。
                    int count = _serialPort.Read(temp, 0, temp.Length);
                    if (count > 0)
                    {
                        var data = temp.Take(count).ToArray();
                        _dataReceivedBuffer.AddRange(data);
                        DataReadFromInBuffer?.Invoke(this, new SerialEventArgs(data));
                    }

                    FilterCompletedPackages(_dataReceivedBuffer.ToArray(), () => _serialPort.BytesToRead > 0,
                        out int[] indexes);

                    if (indexes != null && indexes.Length > 0)
                    {
                        Array.Sort(indexes);
                        for (int i = 0; i < indexes.Length; i++)
                        {
                            var frame = _dataReceivedBuffer.Take(indexes[i] + 1).ToArray();
                            _dataReceivedBuffer.RemoveRange(0, indexes[i] + 1);
                            _replayEvent.Set();// 这个位置还需要考虑合不合适
                            CompletedPackageReceived?.Invoke(this, new SerialEventArgs(frame));
                        }
                    }

                    if (_resetFlag)
                    {
                        break;
                    }

                    // ReSharper disable once AccessToDisposedClosure
                } while (SpinWait.SpinUntil(() => _serialPort.BytesToRead > 0, 5));

                if (_resetFlag)
                {
                    _dataReceivedBuffer.Clear();
                    _serialPort?.Dispose();
                    _serialPort = null;
                    _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                    _serialPort.RtsEnable = EnableRts;
                    _serialPort.Handshake = Handshake;
                    _serialPort.Open();
                    _resetFlag = false;
                    _resetPortEvent.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _resetPortEvent.Set();
            }
            finally
            {
                _timer.Change(TimeSpan.FromMilliseconds(25), Timeout.InfiniteTimeSpan);
            }
        }

        #region Disposable

        ~SerialBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _replayEvent?.Dispose();
                _resetPortEvent?.Dispose();
                _timer?.Dispose();
                _serialPort?.Dispose();
            }
        }

        #endregion Disposable

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