using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace NKit.Uart
{
    public abstract class RequestReplyDeviceBase : IDisposable
    {
        #region Fields

        private readonly List<byte> _dataReceivedBuffer;
        private readonly AutoResetEvent _resetPortEvent;
        private readonly Timer _timer;
        private readonly object _transmissionLocker;
        private readonly AutoResetEvent _waitResponseEvent;
        private int _resetBaudRate;
        private int _resetDataBits;
        private bool _resetFlag;
        private Handshake _resetHandshake;
        private Parity _resetParity;
        private string _resetPortName;
        private bool _resetRtsEnable;
        private StopBits _resetStopBits;
        private SerialPort _serialPort;
        private byte[] _lastDataSent;
        private byte[] _completedFrame;

        #endregion Fields

        protected RequestReplyDeviceBase(string portName, int baudRate, Parity parity, StopBits stopBits) : this(portName, baudRate, parity, stopBits, 8, false, Handshake.None)
        {
        }

        protected RequestReplyDeviceBase(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, bool rtsEnable, Handshake handshake)
        {
            _resetPortName = portName;
            _resetBaudRate = baudRate;
            _resetParity = parity;
            _resetStopBits = stopBits;
            _resetDataBits = dataBits;
            _resetHandshake = handshake;
            _resetRtsEnable = rtsEnable;
            _dataReceivedBuffer = new List<byte>();
            _waitResponseEvent = new AutoResetEvent(false);
            _resetPortEvent = new AutoResetEvent(false);
            _transmissionLocker = new object();
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.RtsEnable = rtsEnable;
            _serialPort.Handshake = handshake;
            TimerPeriod = 20;
            _timer = new Timer(CallBack);
            _timer.Change(0, Timeout.Infinite);
        }

        protected object Tag { get; set; }

        protected int LastTimeCompletedFrameResolved { get; private set; }

        protected int TimerPeriod { get; }

        protected int OpenSerialPortTime { get; set; } = 300;

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

        protected abstract bool FilterCompletedFrame(byte[] lastDataSent, byte[] dataReceivedBuffer, Func<bool> hasRemainingBytesInReadBuffer);

        protected int CalculateTransmissionTime(int byteCount)
        {
            return (int)Math.Ceiling(10000d / BaudRate * byteCount);
        }

        #region Events

        public event Action<SerialEventArgs<byte[]>> CompletedFrameReceived;

        /// <summary>
        /// 用于调试串口，强烈建议注册程序只是打印日志
        /// </summary>
        public event Action<SerialEventArgs<byte[]>> DataRead;

        public event Action<SerialEventArgs<byte[]>> DataSent;

        public event Action<SerialEventArgs<Exception>> TimedDataReadingJobThrowException;

        #endregion Events

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

        protected Response<byte[]> Request(byte[] bytes, int waitResponseTimeout = 100)
        {
            if (waitResponseTimeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(waitResponseTimeout), "The argument should be greater than 0");
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            lock (_transmissionLocker)
            {
                try
                {
                    // 定时器负责打开串口。如果调用过Reset(),Reset()结束后，TimerPeriod毫秒后串口才会被打开，但是如果Reset()结束立刻Request()夺到_transmissionLocker锁，这个时候串口并没有打开。所以在这里等待TimerPeriod * 2 * 10 毫秒。
                    if (!_serialPort.IsOpen)
                    {
                        int i;
                        for (i = 0; i < 5 && !_serialPort.IsOpen; i++)
                        {
                            Thread.Sleep(TimerPeriod + (int)Math.Round(OpenSerialPortTime / 3d));
                        }
                        if (i >= 5)
                            throw new InvalidOperationException("Port isn't open and that may have been occupied by another process.");
                    }

                    // 发送数据
                    _serialPort.WriteTimeout = waitResponseTimeout;
                    _waitResponseEvent.Reset();
                    var start = Environment.TickCount;
                    _serialPort.Write(bytes, 0, bytes.Length);
                    var end = Environment.TickCount;
                    _lastDataSent = bytes.ToArray();
                    DataSent?.Invoke(new SerialEventArgs<byte[]>(bytes, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));

                    // 等待响应
                    var timeout = !_waitResponseEvent.WaitOne(waitResponseTimeout - (end - start));
                    return timeout ? new Response<byte[]>(_dataReceivedBuffer.ToArray(), "Response timeout. Maybe no data was received or received data can't be resolved a completed Frame.") : new Response<byte[]>(_completedFrame.ToArray());
                }
                catch (Exception exception)
                {
                    return new Response<byte[]>(_dataReceivedBuffer.ToArray(), exception);
                }
            }
        }

        // 注意Read不能死等！不要在其他线程轻易调用DiscardIn/OutBuff,会造成Read死等。
        // Read阻塞型API. 直到缓冲区至少有1个字节可读便返回，如果长时间缓冲区为0，则一直阻塞，直到超时抛出TimeoutException,如果Timeout值是-1，则一直阻塞。
        // temp.Length - offset 如果是0，表示不读取，任何情况下Read都会立刻返回，不存在阻塞情况。
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
                        _serialPort.ReadTimeout = 100;
                    }

                    // ② 如果需要,会重置串口
                    if (_resetFlag)
                    {
                        break;
                    }

                    // ③ 如果OS Buffer有数据，则全部读出来
                    var temp = new byte[_serialPort.BytesToRead];
                    int count = _serialPort.Read(temp, 0, temp.Length); // Read不会阻塞，因为肯定有数据。
                    if (count > 0) // 缓存区有N个字节，但实际可能只读到(N-x)个字节(0<=x<=N)
                    {
                        var data = temp.Take(count).ToArray();
                        _dataReceivedBuffer.AddRange(data);
                        DataRead?.Invoke(new SerialEventArgs<byte[]>(data, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                    }

                    // ④ 从应用层接收缓存解析出完整帧。如果有完整帧，会执行帧处理事件
                    bool success = FilterCompletedFrame(_lastDataSent, _dataReceivedBuffer.ToArray(), () => _serialPort.BytesToRead > 0);
                    if (success)
                    {
                        _completedFrame = _dataReceivedBuffer.ToArray();
                        _waitResponseEvent.Set();
                        LastTimeCompletedFrameResolved = Environment.TickCount;
                        CompletedFrameReceived?.Invoke(new SerialEventArgs<byte[]>(_dataReceivedBuffer.ToArray(), Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                        _dataReceivedBuffer.Clear();
                    }

                    //缓存为空分2种情况：
                    //①.刚解析出一个或多个完整帧。这种情况下，绝大概率几毫秒内不会再接收到一个完整帧甚至不会接收到任何数据，跳出循环可以避免循环条件中不必要的CPU自旋耗时。
                    //②.对方长时间不向己方发送数据。这种情况下，定时任务完全没必要执行自旋等待。
                    if (_dataReceivedBuffer.Count == 0)
                    {
                        // 本次定时器任务，解析出一个完整帧，或者未接收任何数据，跳出，等待下一个定时器到达。否则，继续在本次定时器任务中解析完整帧
                        // 根据实际测试，99.999%的情况下，SerialPort.Read会全部读出一个帧，所以几乎总是break，虽然有循环，但是
                        // 几乎不会发生CPU自旋
                        break;
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    // 操作系统层接收缓存有未读出的数据或5个字节时间之内有新数据到达，则在本次定时任务继续执行上述4个任务。
                    // (确定还有未处理的数据时，这样做相比于重新等下一次定时器抵达，处理数据更加及时)
                } while (SpinWait.SpinUntil(() => _serialPort.BytesToRead > 0, (CalculateTransmissionTime(3) > 5 ? 5 : CalculateTransmissionTime(3)) < 2 ? 2 : CalculateTransmissionTime(3) > 5 ? 5 : CalculateTransmissionTime(3))); // 小于2时强制置2，大于5时强制置5
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
                    _timer.Change(TimeSpan.FromMilliseconds(0), Timeout.InfiniteTimeSpan);
                }
                else
                {
                    _timer.Change(TimeSpan.FromMilliseconds(TimerPeriod), Timeout.InfiniteTimeSpan);
                }
            }
        }

        public class SerialEventArgs<T> : EventArgs
        {
            public SerialEventArgs(T data, object tag, string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, bool rtsEnable, Handshake handshake)
            {
                Data = data;
                Tag = tag;
                PortName = portName;
                BaudRate = baudRate;
                DataBits = dataBits;
                StopBits = stopBits;
                Parity = parity;
                RtsEnable = rtsEnable;
                Handshake = handshake;
            }

            public int BaudRate { get; }
            public T Data { get; }
            public int DataBits { get; }
            public Handshake Handshake { get; }
            public Parity Parity { get; }
            public string PortName { get; }
            public bool RtsEnable { get; }
            public StopBits StopBits { get; }
            public object Tag { get; }
        }

        #region Disposable

        ~RequestReplyDeviceBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _waitResponseEvent?.Dispose();
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