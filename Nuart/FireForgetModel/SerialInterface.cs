using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace Nuart.FireForgetModel
{
    public sealed class SerialInterface<TReceiveFilter> : IDisposable where TReceiveFilter : IReceiveFilter, new()
    {
        #region Fields

        private readonly List<byte> _dataReceivedBuffer;
        private readonly AutoResetEvent _resetPortEvent;
        private readonly Timer _timer;
        private readonly object _transmissionLocker;
        private int _resetBaudRate;
        private int _resetDataBits;
        private bool _resetFlag;
        private Handshake _resetHandshake;
        private Parity _resetParity;
        private string _resetPortName;
        private bool _resetRtsEnable;
        private StopBits _resetStopBits;
        private SerialPort _serialPort;
        private int interval;
        private TReceiveFilter receiveFilter;
        #endregion Fields

        #region Constructors
        public SerialInterface(string portName) : this(portName, 9600)
        {
        }

        public SerialInterface(string portName, int baudRate) : this(portName, baudRate, Parity.None, StopBits.One)
        {
        }

        public SerialInterface(string portName, int baudRate, Parity parity, StopBits stopBits) : this(portName, baudRate, parity, stopBits, 8, false, Handshake.None)
        {
        }

        public SerialInterface(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake)
        {
            _resetPortName = portName;
            _resetBaudRate = baudRate;
            _resetParity = parity;
            _resetStopBits = stopBits;
            _resetDataBits = dataBits;
            _resetHandshake = handshake;
            _resetRtsEnable = rtsEnable;
            _dataReceivedBuffer = new List<byte>();
            _resetPortEvent = new AutoResetEvent(false);
            _transmissionLocker = new object();
            receiveFilter = new TReceiveFilter();
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.RtsEnable = rtsEnable;
            _serialPort.Handshake = handshake;
            interval = 25;
            _timer = new Timer(CallBack);
            _timer.Change(0, Timeout.Infinite);
        }

        #endregion

        #region Properties
        /// <summary>
        /// 可用于做判EOCH HeartBeat IsConnect
        /// </summary>
        public int LastCompletedFrameResolvedTime { get; private set; }

        public object Tag { get; set; }
        #region Communication Options

        public int BaudRate => _serialPort.BaudRate;

        public int DataBits => _serialPort.DataBits;

        /// <summary>
        /// 表示己方在发送数据前是否检查对方此刻允不允许发送数据。
        /// </summary>
        public Handshake Handshake => _serialPort.Handshake;

        public Parity Parity => _serialPort.Parity;

        public string PortName => _serialPort.PortName;

        /// <summary>
        /// True表示设备可以接收数据。这个信号只是决定输出信号给对方，允许对方随时可以向己方发送数据，对方用不用不管。
        /// </summary>
        public bool RtsEnable => _serialPort.RtsEnable;

        public StopBits StopBits => _serialPort.StopBits;

        #endregion Communication Options 
        #endregion

        public Response Fire(byte[] bytes, int writeTimeout = 100)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (writeTimeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(writeTimeout), "The argument should be greater than 0");
            }

            lock (_transmissionLocker)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                    {
                        int i;
                        for (i = 1; i <= 7 && !_serialPort.IsOpen; i++)
                        {
                            Thread.Sleep(i * 20);
                        }
                        if (i > 7)
                            throw new InvalidOperationException("Port isn't open and that may have been occupied by another process.");
                    }

                    _serialPort.WriteTimeout = writeTimeout;
                    _serialPort.Write(bytes, 0, bytes.Length);
                    DataSent?.Invoke(new SerialEventArgs<byte[]>(bytes, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                    return new Response();
                }
                catch (Exception exception)
                {
                    return new Response(exception);
                }
            }
        }

        public void Reset(string portName = null, int? baudRate = null, Parity? parity = null, StopBits? stopBits = null, int? dataBits = null, bool? rtsEnable = null, Handshake? handshake = null)
        {
            lock (_transmissionLocker)
            {
                if (portName != null)
                {
                    _resetPortName = portName;
                }

                if (baudRate.HasValue)
                {
                    _resetBaudRate = baudRate.Value;
                }

                if (parity.HasValue)
                {
                    _resetParity = parity.Value;
                }

                if (stopBits.HasValue)
                {
                    _resetStopBits = stopBits.Value;
                }

                if (dataBits.HasValue)
                {
                    _resetDataBits = dataBits.Value;
                }

                if (handshake.HasValue)
                {
                    _resetHandshake = handshake.Value;
                }

                if (rtsEnable.HasValue)
                {
                    _resetRtsEnable = rtsEnable.Value;
                }
                _resetPortEvent.Reset();
                _resetFlag = true;
                _resetPortEvent.WaitOne();
            }
        }


        #region Events

        public event Action<SerialEventArgs<byte[]>> CompletedPackageReceived;

        /// <summary>
        /// 用于调试串口，强烈建议注册程序只是打印日志
        /// </summary>
        public event Action<SerialEventArgs<byte[]>> DataRead;

        public event Action<SerialEventArgs<byte[]>> DataSent;

        public event Action<SerialEventArgs<Exception>> TimedDataReadingJobThrowException;

        #endregion Events

        private void CallBack(object state)
        {
            try
            {
                do
                {
                    // ① 如果串口未打开，则打开串口
                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                    }
                    // ② 如果OS Buffer有数据，则全部读出来
                    var bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        var temp = new byte[_serialPort.BytesToRead];
                        int count = _serialPort.Read(temp, 0, temp.Length); // Read不会阻塞，因为肯定有数据。
                        if (count > 0) // 缓存区有N个字节，但实际可能只读到<N个字节
                        {
                            var data = temp.Take(count).ToArray();
                            _dataReceivedBuffer.AddRange(data);
                            DataRead?.Invoke(new SerialEventArgs<byte[]>(data, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                        }
                    }

                    // ③ 从应用层接收缓存解析出所有完整帧。如果有完整帧，会执行帧处理事件
                    receiveFilter.FilterCompletedFrames(_dataReceivedBuffer.ToArray(), out var indexes, ()=>_serialPort.BytesToRead > 0
                        );
                    if (indexes != null && indexes.Length > 0)
                    {
                        Array.Sort(indexes);
                        for (var i = 0; i < indexes.Length; i++)
                        {
                            var package = _dataReceivedBuffer.Take(indexes[i] + 1).ToArray();
                            _dataReceivedBuffer.RemoveRange(0, indexes[i] + 1);
                            CompletedPackageReceived?.Invoke(new SerialEventArgs<byte[]>(package, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                        }
                        LastCompletedFrameResolvedTime = Environment.TickCount;
                    }

                    // ④ 如果需要会重置串口
                    if (_resetFlag)
                    {
                        break;
                    }

                    //缓存为空分2种情况：
                    //①.刚解析出一个或多个完整帧。这种情况下，绝大概率几毫秒内不会再接收到一个完整帧甚至不会接收到任何数据，跳出循环可以避免循环条件中不必要的CPU自旋耗时。
                    //②.对方长时间不向己方发送数据。这种情况下，定时任务完全没必要执行自旋等待。
                    if (_dataReceivedBuffer.Count == 0)
                    {
                        break;
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    // 操作系统层接收缓存有未读出的数据或5个字节时间之内有新数据到达，则在本次定时任务继续执行上述4个任务。
                    // (确定还有未处理的数据时，这样做相比于重新等下一次定时器抵达，处理数据更加及时)
                } while (SpinWait.SpinUntil(() => _serialPort.BytesToRead > 0, (CalculateTransmissionTime(3) > 5 ? 5 : CalculateTransmissionTime(3)) < 2 ? 2 : CalculateTransmissionTime(3) > 5 ? 5 : CalculateTransmissionTime(3))); // 小于2ms时强制置2，大于5ms时强制置5
            }
            catch (Exception e)
            {
                TimedDataReadingJobThrowException?.Invoke(new SerialEventArgs<Exception>(e, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
            }
            finally
            {
                // 放到finally，避免try块种出现异常的情况下Reset()一直阻塞。
                if (_resetFlag)
                {
                    _dataReceivedBuffer.Clear();
                    _serialPort?.Dispose();
                    _serialPort = null;
                    _serialPort = new SerialPort(_resetPortName, _resetBaudRate, _resetParity, _resetDataBits, _resetStopBits);
                    _serialPort.RtsEnable = _resetRtsEnable;
                    _serialPort.Handshake = _resetHandshake;
                    _resetFlag = false;
                    _resetPortEvent.Set();
                }
                _timer.Change(TimeSpan.FromMilliseconds(interval), Timeout.InfiniteTimeSpan);
            }
        }

        private int CalculateTransmissionTime(int byteCount)
        {
            return (int)Math.Ceiling(10000d / BaudRate * byteCount);
        }


        #region Disposable

        ~SerialInterface()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _resetPortEvent?.Dispose();
                _timer?.Dispose();
                _serialPort?.Dispose();
            }
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        #endregion Disposable
    }
}