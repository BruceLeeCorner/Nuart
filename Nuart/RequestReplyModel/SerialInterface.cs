﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace Nuart.RequestReplyModel
{
    public sealed class SerialInterface<TReceiveFilter> : IDisposable where TReceiveFilter : IReceiveFilter, new()
    {
        #region Fields

        private readonly List<byte> _dataReceivedBuffer;
        private readonly AutoResetEvent _resetPortEvent;
        private readonly Timer _timer;
        private readonly object _transmissionLocker;
        private readonly AutoResetEvent _waitResponseEvent;
        private byte[] _completedFrame;
        private byte[] _lastDataSent;
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

        public SerialInterface(string portName, int baudRate, Parity parity, StopBits stopBits) : this(portName, baudRate, parity, stopBits, 8)
        {
        }

        public SerialInterface(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits) : this(portName, baudRate, parity, stopBits, dataBits, false, Handshake.None)
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
            _waitResponseEvent = new AutoResetEvent(false);
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

        #endregion Constructors

        #region Events

        /// <summary>
        /// 用于调试串口，强烈建议注册的事件处理程序只是打印日志
        /// </summary>
        public event Action<SerialEventArgs<byte[]>> CompletedFrameReceived;

        public event Action<SerialEventArgs<byte[]>> DataRead;

        public event Action<SerialEventArgs<byte[]>> DataSent;

        public event Action<SerialEventArgs<Exception>> TimedDataReadingJobThrowException;

        #endregion Events

        #region Properties

        public object Tag { get; set; }

        /// <summary>
        /// 可用于做判EOCH HeartBeat IsConnect
        /// </summary>
        public int LastCompletedFrameResolvedTime { get; private set; }

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

        #endregion Properties

        public Response<byte[]> Request(byte[] bytes, int waitResponseTimeout = 200)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (waitResponseTimeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(waitResponseTimeout), "The argument should be greater than 0");
            }

            lock (_transmissionLocker)
            {
                try
                {
                    // 定时器负责打开串口。如果Reset()结束后Request()立刻夺到_transmissionLocker锁，
                    // 这个时候串口可能并没有打开。经测试Moxa串口服务器的虚拟串口Open最大耗时是250ms,
                    // 另外，考虑到Moxa是等级产品，其他品牌的虚拟串口的Open可能会更久，所以这里使用的最长等待
                    // 时长是560ms。
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

                    // 发送数据
                    _serialPort.WriteTimeout = waitResponseTimeout;
                    _waitResponseEvent.Reset();
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    _serialPort.Write(bytes, 0, bytes.Length);
                    _lastDataSent = bytes.ToArray();
                    DataSent?.Invoke(new SerialEventArgs<byte[]>(bytes, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                    stopwatch.Stop();
                    // 等待响应
                    var timeout = !_waitResponseEvent.WaitOne(waitResponseTimeout - (int)stopwatch.ElapsedMilliseconds);
                    return timeout ? new Response<byte[]>(_dataReceivedBuffer.ToArray(), "Response timeout. Maybe no data was received or received data can't be resolved a completed Frame.") : new Response<byte[]>(_completedFrame.ToArray());
                }
                catch (Exception exception)
                {
                    return new Response<byte[]>(_dataReceivedBuffer.ToArray(), exception);
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

        private int CalculateTransmissionTime(int byteCount)
        {
            return (int)Math.Ceiling(10000d / BaudRate * byteCount);
        }

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
                    var bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        var temp = new byte[bytesToRead];
                        int count = _serialPort.Read(temp, 0, temp.Length); // Read不会阻塞，因为肯定有数据。
                        if (count > 0) // 缓存区有N个字节，但实际可能只读到(N-x)个字节(0<=x<=N)
                        {
                            var data = temp.Take(count).ToArray();
                            _dataReceivedBuffer.AddRange(data);
                            DataRead?.Invoke(new SerialEventArgs<byte[]>(data, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                        }
                    }
                    // ④ 从应用层接收缓存解析出完整帧。如果有完整帧，会执行帧处理事件
                    bool success = receiveFilter.IsCompletedFrame(_lastDataSent, _dataReceivedBuffer.ToArray(), ()=> _serialPort.BytesToRead > 0);
                    if (success)
                    {
                        _completedFrame = _dataReceivedBuffer.ToArray();
                        _waitResponseEvent.Set();
                        CompletedFrameReceived?.Invoke(new SerialEventArgs<byte[]>(_completedFrame, Tag, PortName, BaudRate, DataBits, StopBits, Parity, RtsEnable, Handshake));
                        _dataReceivedBuffer.Clear();
                        LastCompletedFrameResolvedTime = Environment.TickCount;
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
                    _timer.Change(TimeSpan.FromMilliseconds(0), Timeout.InfiniteTimeSpan);
                }
                else
                {
                    _timer.Change(TimeSpan.FromMilliseconds(interval), Timeout.InfiniteTimeSpan);
                }
            }
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