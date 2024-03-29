﻿using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

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

        #region 连接状态

        public bool IsConnected { get; set; }

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

        public void Connect()
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

                //if (!string.IsNullOrWhiteSpace(RemoteIp))
                //{
                //    _udpClient.Connect(RemoteIp, RemotePort);
                //}
            }
        }

        private async Task StartReceiving(uint jobId)
        {
            while (IsWorking && jobId == _jobId && _udpClient != null)
            {
                //if (_udpClient is not null)
                //{
                //    var udpReceiveResult = await _udpClient.ReceiveAsync();
                //    IsConnected  = true;
                //    ErrorMessage = string.Empty;
                //    try
                //    {
                //        DataReceived?.Invoke(this, udpReceiveResult.Buffer);
                //    }
                //    catch (Exception e)
                //    {
                //        // ignored
                //    }
                //}


                //=====================================================================================

                Task<UdpReceiveResult> receiveTask;
                lock (_reloadingLock)
                {
                    receiveTask = _udpClient.ReceiveAsync();
                }

                var timeoutTask = Task.Delay(TimeoutInMilliseconds);
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == receiveTask)
                {
                    var udpReceiveResult = receiveTask.Result;
                    IsConnected = true;
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
                    IsConnected = false;
                    if (IsWorking)
                    {
                        ErrorMessage = "接收超时";
                    }

                    break;
                }
            }
        }

        public void Disconnect()
        {
            IsWorking    = false;
            ErrorMessage = string.Empty;
        }


        public void SendData(byte[] data)
        {
            if (IsWorking)
            {
                lock (_reloadingLock)
                {
                    _udpClient?.Send(data, data.Length,RemoteIp,RemotePort);
                }
            }
            else
            {
                throw new ApplicationException("处于非工作模式,无法发送数据.");
            }
        }
    }
}