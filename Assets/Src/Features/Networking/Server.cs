using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Src.Features.Networking
{
    /// <summary>
    /// Объект сервера
    /// </summary>
    public class Server: IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private int _maxConnection;
        private int _reliableChannel;
        private int _unreliableChannel;
        private int _hostId;
        private bool _isStarted;
        private IDictionary<int, string> _connectedUsers = new Dictionary<int, string>();
        
        public Server(int networkClientMaxConnection)
        {
            _maxConnection = networkClientMaxConnection;
        }

        /// <summary>
        /// Запустить сервер
        /// </summary>
        /// <param name="port">Порт сервера</param>
        public void Start(int port, bool isWebsockket)
        {
            NetworkTransport.Init();
            ConnectionConfig cc = new ConnectionConfig();
            _reliableChannel = cc.AddChannel(QosType.Reliable);
            _unreliableChannel = cc.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(cc, _maxConnection);
            if (isWebsockket)
                _hostId = NetworkTransport.AddWebsocketHost(topology, port, "127.0.0.1");
            else
                _hostId = NetworkTransport.AddHost(topology, port);
            _isStarted = true;
            _cancellationTokenSource = new CancellationTokenSource();
            ReceiveMessageWaiter(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Остановить сервер
        /// </summary>
        public void Stop()
        {
            if (!_isStarted) return;

            NetworkTransport.RemoveHost(_hostId);
            NetworkTransport.Shutdown();
            _cancellationTokenSource.Cancel();
            _isStarted = false;
        }
        
        /// <summary>
        /// Отправить сообщение подключенному пользователю
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="connectionID">ID подключения</param>
        /// <param name="isReliable">Гарантированная доставка сообщения</param>
        public void SendMessage(string message, int connectionID, bool isReliable)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);
            NetworkTransport.Send(_hostId, connectionID, isReliable ? _reliableChannel : _unreliableChannel, buffer, message.Length * sizeof(char), out byte error);
            #if UNITY_EDITOR
            if ((NetworkError)error != NetworkError.Ok) Debug.Log((NetworkError)error);
            #endif
        }
        
        /// <summary>
        /// Отправить сообщение всем пользователям
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="isReliable">Гарантированная доставка сообщения</param>
        public void SendMessageToAll(string message, bool isReliable)
        {
            int[] ids = _connectedUsers.Keys.ToArray();
            
            for (int i = 0; i < ids.Length; i++)
            {
                SendMessage(message, ids[i], isReliable);
            }
        }

        private async void ReceiveMessageWaiter(CancellationToken token)
        {
            if (!_isStarted) return;

            int recHostId;
            int connectionId;
            int channelId;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
            int dataSize;
            
            while (!token.IsCancellationRequested)
            {
                NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId,
                    recBuffer, bufferSize, out dataSize, out byte error);
                switch (recData)
                {
                    case NetworkEventType.Nothing:
                        break;

                    case NetworkEventType.ConnectEvent:
                        _connectedUsers.Add(connectionId, string.Empty);
                        Debug.LogFormat("{0} is connecting...", connectionId);
                        break;

                    case NetworkEventType.DataEvent:
                        string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                        if (string.IsNullOrEmpty(_connectedUsers[connectionId]))
                        {
                            _connectedUsers[connectionId] = message;
                            Debug.LogFormat(message + " enter the chat");
                            SendMessageToAll($"{message} enter the chat", true);
                            break;
                        }

                        Debug.LogFormat("{0}: {1}", _connectedUsers[connectionId], message);
                        SendMessageToAll($"{_connectedUsers[connectionId]}: {message}", true);
                        break;

                    case NetworkEventType.DisconnectEvent:
                        SendMessageToAll($"{_connectedUsers[connectionId]} has disconnected.", true);
                        Debug.LogFormat("{0} has disconnected.", _connectedUsers[connectionId]);
                        _connectedUsers.Remove(connectionId);
                        break;

                    case NetworkEventType.BroadcastEvent:
                        break;
                }

                await Task.Yield();
            }
            token.ThrowIfCancellationRequested();
        }


        public void Dispose()
        {
            if(_isStarted)
                Stop();
            _cancellationTokenSource.Dispose();
        }
    }
}