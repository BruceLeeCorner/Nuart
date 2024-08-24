using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace NKit.Uart
{


    public abstract class SerialBase : IDisposable
    {
        public class SerialEventArgs : EventArgs
        {
            public SerialEventArgs(byte[] data)
            {
                Data = data;
            }

            public byte[] Data { get; }
        }


        private readonly object _locker = new object();

        #region Fileds

        private static readonly byte[] EmptyByteArray = new byte[0];

        // ReSharper disable once IdentifierTypo
        private readonly object _transmitlocker = new object();

        private readonly object _openPortLocker = new object();

        private bool _willNotify;

        #region Reset These Variables Before Transmition

        private  AutoResetEvent _waitReplyEvent;
        private readonly Stopwatch _stopWatch;

        /// <summary>
        /// 响应超时时长
        /// </summary>
        private int _replyTimeout = 200;

        /// <summary>
        /// 最后一次发送请求消息的时刻
        /// </summary>
        private DateTime? _latestWaitReplyEndingTime;

        private DateTime? _latestReceiveExpectedReplyTime;
        private string _errorContent;
        private byte[] _receivedData;
        private byte[] _sendingBytes;
        private bool _disposedValue;


        #endregion Reset These Variables Before Transmition

        private SerialPort _port;

        #endregion Fileds

        #region Properties

        /// <summary>
        /// 串口名称，如COM1,COM2,COM3...
        /// </summary>
        public string PortName { get; }

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; }

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity { get; }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; }

        /// <summary>
        /// 发送和接收一个串口帧(也就是一个有效字节)的耗时
        /// </summary>
        public int OneByteTransmissionTime => (int)Math.Ceiling(10000d / BaudRate);

        /// <summary>
        /// 收到响应或响应超时后，间隔多久再发送下次请求。
        /// 小于或等于0，表示不延时，即可发送下次请求。
        /// </summary>
        public int NextRequestInterval { get; set; }

        #endregion Properties

        private EventHandler<SerialEventArgs> _dataSent;
        public event EventHandler<SerialEventArgs> DataSent
        {
            add => _dataSent += value;
            remove => _dataSent -= value;
        }

        private EventHandler<SerialEventArgs> _dataReceived;
        public event EventHandler<SerialEventArgs> DataReceived
        {
            add => _dataReceived += value;
            remove => _dataReceived -= value;
        }

        // 单链接，长链接
        protected SerialBase(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            StopBits = stopBits;
            DataBits = dataBits;
            _port = new SerialPort();
            _port.PortName = portName;
            _port.BaudRate = baudRate;
            _port.DataBits = dataBits;
            _port.Parity = parity;
            _port.StopBits = stopBits;
            _port.Handshake = Handshake.None;
            _port.RtsEnable = false;
            _port.DtrEnable = false;
            _port.DataReceived += _port_DataReceived;
            _errorContent = string.Empty;
            _receivedData = EmptyByteArray;
            _sendingBytes = EmptyByteArray;
            _latestWaitReplyEndingTime = null;
            _stopWatch = new Stopwatch();
            _waitReplyEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// 判断接收缓存内的所有字节是不是一个完整且合法的响应。
        /// </summary>
        /// <param name="bytesHasRead">当前已从接收到的字节</param>
        /// <param name="requestBytes">请求协议包</param>
        /// <param name="bytesLengthToRead">接收缓存待读的字节</param>
        /// <param name="checkTimes">当前是第几次检查。返回false，会立刻再次判断接收缓存有无新接收到的字节，有则读取出来再次判断，无则立刻判断。建议加延时。</param>
        /// <returns></returns>
        protected abstract bool IsExpectedReply(byte[] requestBytes, byte[] bytesHasRead, int bytesLengthToRead, int checkTimes);

        private bool _needReset;
        public void Abort()
        {
            this._dataReceived = null;
            this._dataSent = null;
            _port.DataReceived -= _port_DataReceived;
            _port?.Dispose();
            _port = null;
            _waitReplyEvent?.Dispose();
            _waitReplyEvent = null;
            //_port = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
            //_waitReplyEvent = new AutoResetEvent(false);
        }

        protected Response<byte[]> Request(byte[] bytes, int replyTimeout = 200)
        {
            if (_needReset)
            {
                _port?.Dispose();
                _waitReplyEvent?.Dispose();
                _needReset = false;

                _port = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                _waitReplyEvent = new AutoResetEvent(false);
            }


            try
            {
                // 打开串口
                Open();

                lock (_transmitlocker)
                {
                    // 检查发送请求的时间间隔
                    if (_latestWaitReplyEndingTime != null)
                    {
                        if (NextRequestInterval > 0)
                        {
                            double interval = (DateTime.Now - _latestWaitReplyEndingTime.Value).TotalMilliseconds;
                            if (interval < NextRequestInterval)
                            {
                                Thread.Sleep((int)Math.Ceiling(NextRequestInterval - interval));
                            }
                        }
                    }

                    // 重置
                    _sendingBytes = bytes.ToArray();
                    _replyTimeout = replyTimeout;
                    _waitReplyEvent.Reset();
                    _errorContent = string.Empty;
                    _receivedData = EmptyByteArray;
                    _willNotify = false;
                    _port.DiscardOutBuffer();
                    _port.DiscardInBuffer();
                    _port.Write(bytes, 0, bytes.Length);
                    // 无需等待
                    if (replyTimeout <= 0)
                    {
                        DataReceived?.Invoke(this, new SerialEventArgs(_receivedData));
                        return new Response<byte[]>(_receivedData, string.Empty);
                    }
                    _stopWatch.Restart();
                    DataSent?.Invoke(this, new SerialEventArgs(bytes));
                    bool timeout = !_waitReplyEvent.WaitOne(_replyTimeout);
                    if (timeout && _willNotify)
                    {
                        timeout = !_waitReplyEvent.WaitOne(_replyTimeout);
                    }
                    _latestWaitReplyEndingTime = DateTime.Now;

                    byte[] returnReceivedData;
                    string returnErrorContent;
                    if (timeout == false)
                    {
                        returnErrorContent = _errorContent;
                        returnReceivedData = _receivedData.ToArray();
                    }
                    else
                    {
                        returnErrorContent = "Response timeout.";
                        returnReceivedData = _receivedData.ToArray();
                    }
                    DataReceived?.Invoke(this, new SerialEventArgs(returnReceivedData));
                    return new Response<byte[]>(returnReceivedData, returnErrorContent);
                }
            }
            catch (Exception ex)
            {
                var returnReceivedData = _receivedData.ToArray();
                DataReceived?.Invoke(this, new SerialEventArgs(returnReceivedData));
                return new Response<byte[]>(returnReceivedData, ex);
            }
        }


        protected abstract bool IsOnLine();

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public bool IsOnLine(int expiredTimeout = 1000) =>
            expiredTimeout > 0 && _latestReceiveExpectedReplyTime.HasValue &&
            ((DateTime.Now - _latestReceiveExpectedReplyTime.Value).TotalMilliseconds < expiredTimeout) || IsOnLine();

        // 必须保证方法在有限的时间内能结束，也就是必须要有超时机制，否则Close不掉串口。
        private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;

            // 这个判断很有必要， 因为可能多个线程池线程在并行调用DataReceived事件， 但是只有1个线程拿到锁，其他的线程在排队。
            // 拿到锁的线程把Buffer中的数据读取完毕结束后，唤醒1个等待线程，等待线程第1步先判断有无可读数据，无直接返回， 这样效率比较高。
            if (port.BytesToRead > 0)
            {
                try
                {
                    _willNotify = true;
                    var buff = new List<byte>();
                    int i = 0;
                    do
                    {
                        port.ReadTimeout = _replyTimeout;
                        var temp = new byte[port.BytesToRead];
                        // 注意Read不能死等！不要在其他线程轻易调用DiscardIn/OutBuff,会造成Read死等。
                        int count = port.Read(temp, 0, temp.Length);
                        // Read阻塞型API. 直到缓冲区至少有1个字节可读便返回，如果长时间缓冲区为0，则一直阻塞，直到超时抛出TimeoutException,如果Timeout值是-1，则一直阻塞。
                        // temp.Length - offset 如果是0，表示不读取，任何情况下Read都会立刻返回，不存在阻塞情况。
                        if (count > 0)
                        {
                            buff.AddRange(temp.Take(count));
                            _receivedData = buff.ToArray();
                        }

                        if (IsExpectedReply(_sendingBytes.ToArray(), buff.ToArray(), port.BytesToRead, ++i))
                        {
                            _latestReceiveExpectedReplyTime = DateTime.Now;
                            break;
                        }

                        if (_replyTimeout > 0 && _stopWatch.ElapsedMilliseconds > _replyTimeout)
                        {
                            _errorContent = $"Response timeout.";
                            break;
                        }
                    } while (true);
                }
                catch (Exception exception)
                {
                    _errorContent = exception.ToString();
                }
                finally
                {
                    _stopWatch.Stop();
                    _waitReplyEvent.Set();
                }
            }
        }

        private void Open()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            // ReSharper disable once InvertIf
            if (!_port.IsOpen)
            {
                lock (_openPortLocker)
                {
                    if (!_port.IsOpen)
                    {
                        _port.Open();
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                _port.DataReceived -= _port_DataReceived;
                _waitReplyEvent.Dispose();
                _port.Dispose();
                // TODO: 将大型字段设置为 null
                _disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~SerialBase()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}