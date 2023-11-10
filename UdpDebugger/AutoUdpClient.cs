using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace UdpDebugger
{
    internal class AutoUdpClient
    {
        public bool AutoReconnect { get; set; } = true;

        public int TimeoutInMilliseconds { get; set; } = 1000;

        public string LocalIp   { get; set; } = string.Empty;
        public int    LocalPort { get; set; }


        public string RemoteIp   { get; set; } = string.Empty;
        public int    RemotePort { get; set; }

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler<string>? ErrorChanged;


        public bool IsWorking { get; private set; }


        public AutoUdpClient()
        {
            InitTimer();
        }

        #region 连接状态 & 检测

        public bool IsConnected { get; set; }

        private uint _recvCount       = 0;
        private uint _recvCountBackup = 0;

        #endregion


        #region 连接检测

        private readonly Timer _timer = new(1000);

        private void InitTimer()
        {
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_recvCount > _recvCountBackup)
            {
                IsConnected = true;
            }
            else
            {
                IsConnected = false;
            }

            _recvCountBackup = _recvCount;
        }

        #endregion

        private string _errorMessage = string.Empty;

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                ErrorChanged?.Invoke(this, _errorMessage);
            }
        }

        private uint _jobId;

        private UdpClient? _udpClient = null;

        public void Start()
        {
            if (IsWorking)
            {
                throw new InvalidOperationException("正在工作.需要先停止当前工作,才能重新开始工作.");
            }

            _jobId++;
            var jobId = _jobId;

            IsWorking    = true;
            ErrorMessage = string.Empty;


            Task.Run(async () =>
                     {
                         do
                         {
                             try
                             {
                                 ReloadUdpClient();

                                 await StartReceiving(jobId);
                             }
                             catch (Exception e)
                             {
                                 ErrorMessage = e.Message;
                                 await Task.Delay(1000);
                             }
                         } while (AutoReconnect && IsWorking && jobId == _jobId && !IsConnected); //重试,直至停止工作或连接成功

                         lock (_reloadingLock)
                         {
                             _udpClient?.Dispose();
                         }
                     });
        }

        private readonly object _reloadingLock = new();

        private void ReloadUdpClient()
        {
            lock (_reloadingLock)
            {
                if (_udpClient is not null)
                {
                    _udpClient.Dispose();
                    _udpClient = null;
                }

                var localIpAddress = string.IsNullOrWhiteSpace(LocalIp) ? IPAddress.Any : IPAddress.Parse(LocalIp);
                var localEndPoint  = new IPEndPoint(localIpAddress, LocalPort);

                _udpClient = new UdpClient(localEndPoint);

                if (!string.IsNullOrWhiteSpace(RemoteIp))
                {
                    _udpClient.Connect(RemoteIp, RemotePort);
                }
            }
        }

        private async Task StartReceiving(uint jobId)
        {
            while (IsWorking && jobId == _jobId && _udpClient != null)
            {
                Task<UdpReceiveResult> receiveTask;
                lock (_reloadingLock)
                {
                    receiveTask = _udpClient.ReceiveAsync();
                }

                var timeoutTask   = Task.Delay(TimeoutInMilliseconds);
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == receiveTask)
                {
                    var udpReceiveResult = receiveTask.Result;
                    IsConnected  = true;
                    ErrorMessage = string.Empty;

                    try
                    {
                        DataReceived?.Invoke(this, udpReceiveResult.Buffer);
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                }
                else // 超时
                {
                    IsConnected  = false;
                    ErrorMessage = "接收超时";
                    break;
                }
            }
        }


        public void Stop()
        {
            IsWorking    = false;
            ErrorMessage = string.Empty;
        }
    }
}